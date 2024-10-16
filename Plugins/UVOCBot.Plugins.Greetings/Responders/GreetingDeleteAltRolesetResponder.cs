using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Plugins.Greetings.Responders;

internal sealed class GreetingDeleteAltRolesetResponder : IComponentResponder
{
    private readonly IInteraction _context;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly FeedbackService _feedbackService;
    private readonly DiscordContext _dbContext;

    public GreetingDeleteAltRolesetResponder
    (
        IInteractionContext context,
        IDiscordRestChannelAPI channelApi,
        FeedbackService feedbackService,
        DiscordContext dbContext
    )
    {
        _context = context.Interaction;
        _channelApi = channelApi;
        _feedbackService = feedbackService;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Result<Attribute[]> GetResponseAttributes(string key)
        => Array.Empty<Attribute>();

    /// <inheritdoc />
    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
        => key switch
        {
            GreetingComponentKeys.DeleteAlternateRolesets => await DeleteAlternateRolesetsAsync(ct).ConfigureAwait(false),
            _ => Result.FromError(new GenericCommandError())
        };

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

        Snowflake channelId = _context.Channel.Value.ID.Value;

        if (!memberPerms.HasPermission(DiscordPermission.ManageGuild))
            return new PermissionError(DiscordPermission.ManageGuild, user.ID, channelId);
        if (!memberPerms.HasPermission(DiscordPermission.ManageRoles))
            return new PermissionError(DiscordPermission.ManageRoles, user.ID, channelId);

        if (!_context.Data.Value.TryPickT1(out IMessageComponentData componentData, out _))
            return new GenericCommandError();

        if (!componentData.Values.IsDefined(out IReadOnlyList<string>? selectedValues))
            return new GenericCommandError();

        GuildWelcomeMessage? welcomeMessage = await _dbContext.FindAsync<GuildWelcomeMessage>(guildID.Value, ct)
            .ConfigureAwait(false);

        if (welcomeMessage is null)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                "A guild administrator has removed this role menu",
                ct: ct
            );
        }

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

        await _channelApi.DeleteMessageAsync
        (
            channelId,
            _context.Message.Value.ID,
            ct: ct
        ).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync(sb.ToString(), ct: ct).ConfigureAwait(false);
    }
}
