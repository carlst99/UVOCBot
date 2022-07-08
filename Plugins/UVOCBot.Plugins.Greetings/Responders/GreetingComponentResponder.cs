using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using UVOCBot.Discord.Core.Commands;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Greetings.Abstractions.Services;

namespace UVOCBot.Plugins.Greetings.Responders;

[Ephemeral]
internal sealed class GreetingComponentResponder : IComponentResponder
{
    private readonly IGreetingService _greetingService;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly InteractionContext _context;
    private readonly FeedbackService _feedbackService;
    private readonly DiscordContext _dbContext;

    public GreetingComponentResponder
    (
        IGreetingService greetingService,
        IDiscordRestGuildAPI guildApi,
        InteractionContext context,
        FeedbackService feedbackService,
        DiscordContext dbContext
    )
    {
        _greetingService = greetingService;
        _guildApi = guildApi;
        _context = context;
        _feedbackService = feedbackService;
        _dbContext = dbContext;
    }

    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
        => key switch
        {
            GreetingComponentKeys.NoNicknameMatches => await NoNicknameMatches(ct).ConfigureAwait(false),
            GreetingComponentKeys.SetAlternateRoleset => await SetAlternateRolesetAsync(dataFragment, ct).ConfigureAwait(false),
            GreetingComponentKeys.SetGuessedNickname => await SetGuessedNickname(dataFragment, ct).ConfigureAwait(false),
            GreetingComponentKeys.DeleteAlternateRolesets => await DeleteAlternateRolesetsAsync(ct).ConfigureAwait(false),
            _ => Result.FromError(new GenericCommandError())
        };

    private async Task<Result> NoNicknameMatches(CancellationToken ct)
        => await _feedbackService.SendContextualSuccessAsync
        (
            "Please set your nickname to the name of your PlanetSide 2 character!",
            ct: ct
        ).ConfigureAwait(false);

    private async Task<Result> SetGuessedNickname
    (
        string? dataFragment,
        CancellationToken ct
    )
    {
        if (dataFragment is null)
            return Result.FromSuccess();

        if (!_context.GuildID.IsDefined(out Snowflake guildID))
            return Result.FromSuccess();

        string[] fragmentComponents = dataFragment.Split('@');
        ulong userId = ulong.Parse(fragmentComponents[0]);

        // Check that the user who clicked the button is the focus of the welcome message
        if (_context.User.ID.Value != userId)
        {
            await _feedbackService.SendContextualErrorAsync("Hold it, bud. You can't do that!", ct: ct).ConfigureAwait(false);
            return Result.FromSuccess();
        }

        Result setNickResult = await _guildApi.ModifyGuildMemberAsync(guildID, _context.User.ID, fragmentComponents[1], ct: ct).ConfigureAwait(false);
        if (!setNickResult.IsSuccess)
            return setNickResult;

        IResult alertResponse = await _feedbackService.SendContextualSuccessAsync
        (
            $"Your nickname has been updated to { Formatter.Bold(fragmentComponents[1]) }!",
            ct: ct
        ).ConfigureAwait(false);

        return alertResponse.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(alertResponse.Error!);
    }

    private async Task<Result> SetAlternateRolesetAsync
    (
        string? dataFragment,
        CancellationToken ct
    )
    {
        if (dataFragment is null)
            return Result.FromSuccess();

        if (!_context.GuildID.IsDefined(out Snowflake guildID))
            return Result.FromSuccess();

        if (!_context.Member.IsDefined(out IGuildMember? member))
            return Result.FromSuccess();

        if (!member.User.IsDefined())
            return Result.FromSuccess();

        string[] fragmentComponents = dataFragment.Split('|');
        ulong userID = ulong.Parse(fragmentComponents[0]);
        ulong rolesetID = ulong.Parse(fragmentComponents[1]);

        // Check that the user who clicked the button is the focus of the welcome message
        if (_context.User.ID.Value != userID)
        {
            await _feedbackService.SendContextualErrorAsync("Hold it, bud. You can't do that!", ct: ct).ConfigureAwait(false);
            return Result.FromSuccess();
        }

        Result<IReadOnlyList<ulong>> roleChangeResult = await _greetingService.SetAlternateRolesetAsync
        (
            guildID,
            member,
            rolesetID,
            ct
        ).ConfigureAwait(false);

        if (!roleChangeResult.IsDefined(out IReadOnlyList<ulong>? newRoles))
            return Result.FromError(roleChangeResult);

        // Inform the user of their role change
        string rolesStringList = string.Join(' ', newRoles.Select(Formatter.RoleMention));
        IResult alertResponse = await _feedbackService.SendContextualSuccessAsync
        (
            $"You've been given the following roles: { rolesStringList }",
            ct: ct
        ).ConfigureAwait(false);

        return alertResponse.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(alertResponse.Error!);
    }

    private async Task<Result> DeleteAlternateRolesetsAsync(CancellationToken ct)
    {
        if (!_context.GuildID.IsDefined(out Snowflake guildID))
            return new GenericCommandError();

        if (!_context.Member.IsDefined(out IGuildMember? member))
            return new GenericCommandError();

        if (!member.User.IsDefined(out IUser? user))
            return new GenericCommandError();

        if (!member.Permissions.IsDefined(out IDiscordPermissionSet? memberPerms))
            return new GenericCommandError();

        if (!memberPerms.HasPermission(DiscordPermission.ManageGuild))
            return new PermissionError(DiscordPermission.ManageGuild, user.ID, _context.ChannelID);
        if (!memberPerms.HasPermission(DiscordPermission.ManageRoles))
            return new PermissionError(DiscordPermission.ManageRoles, user.ID, _context.ChannelID);

        if (!_context.Data.TryPickT1(out IMessageComponentData componentData, out _))
            return new GenericCommandError();

        if (!componentData.Values.IsDefined(out IReadOnlyList<string>? selectedValues))
            return new GenericCommandError();

        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(guildID.Value, ct)
            .ConfigureAwait(false);

        List<GuildGreetingAlternateRoleSet> removedRolesets = new();
        foreach (ulong rolesetID in selectedValues.Select(ulong.Parse))
        {
            int removeIndex = welcomeMessage.AlternateRolesets.FindIndex(rs => rs.ID == rolesetID);
            removedRolesets.Add(welcomeMessage.AlternateRolesets[removeIndex]);
            welcomeMessage.AlternateRolesets.RemoveAt(removeIndex);
        }

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        StringBuilder sb = new(Formatter.Bold("The following rolesets have been removed:"));
        sb.AppendLine();
        foreach (GuildGreetingAlternateRoleSet removedRS in removedRolesets)
            sb.Append("- ").AppendLine(removedRS.Description);

        return await _feedbackService.SendContextualSuccessAsync(sb.ToString(), ct: ct).ConfigureAwait(false);
    }
}
