using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Greetings.Abstractions.Services;

namespace UVOCBot.Plugins.Greetings.Responders;

internal sealed class GreetingComponentResponder : IComponentResponder
{
    private readonly IGreetingService _greetingService;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly InteractionContext _context;
    private readonly FeedbackService _feedbackService;

    public GreetingComponentResponder
    (
        IGreetingService greetingService,
        IDiscordRestGuildAPI guildApi,
        InteractionContext context,
        FeedbackService feedbackService
    )
    {
        _greetingService = greetingService;
        _guildApi = guildApi;
        _context = context;
        _feedbackService = feedbackService;
    }

    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
        => key switch
        {
            GreetingComponentKeys.NoNicknameMatches => await NoNicknameMatches(ct).ConfigureAwait(false),
            GreetingComponentKeys.SetAlternateRoles => await SetAlternateRoles(dataFragment, ct).ConfigureAwait(false),
            GreetingComponentKeys.SetGuessedNickname => await SetGuessedNickname(dataFragment, ct).ConfigureAwait(false),
            _ => Result.FromError(new GenericCommandError())
        };

    private async Task<Result> NoNicknameMatches(CancellationToken ct = default)
    {
        IResult alertResponse = await _feedbackService.SendContextualSuccessAsync
        (
            "Please set your nickname to the name of your PlanetSide 2 character!",
            ct: ct
        ).ConfigureAwait(false);

        return !alertResponse.IsSuccess
            ? Result.FromError(alertResponse.Error!)
            : Result.FromSuccess();
    }

    private async Task<Result> SetGuessedNickname
    (
        string? dataFragment,
        CancellationToken ct = default
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

    private async Task<Result> SetAlternateRoles
    (
        string? dataFragment,
        CancellationToken ct = default
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

        // Check that the user who clicked the button is the focus of the welcome message
        ulong userId = ulong.Parse(dataFragment);
        if (_context.User.ID.Value != userId)
        {
            await _feedbackService.SendContextualErrorAsync("Hold it, bud. You can't do that!", ct: ct).ConfigureAwait(false);
            return Result.FromSuccess();
        }

        // Remove the default roles and add the alternate roles
        Result<IReadOnlyList<ulong>> roleChangeResult = await _greetingService.SetAlternateRoles(guildID, member, ct).ConfigureAwait(false);

        if (!roleChangeResult.IsDefined(out IReadOnlyList<ulong>? newRoles))
            return Result.FromError(roleChangeResult);

        // Inform the user of their role change
        string rolesStringList = string.Join(' ', newRoles.Select(r => Formatter.RoleMention(r)));
        IResult alertResponse = await _feedbackService.SendContextualSuccessAsync
        (
            $"You've been given the following roles: { rolesStringList }",
            ct: ct
        ).ConfigureAwait(false);

        return alertResponse.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(alertResponse.Error!);
    }
}
