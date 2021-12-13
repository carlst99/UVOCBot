using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Components;
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

    public async Task<Result> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
    {
        return key switch
        {
            GreetingComponentKeys.NoNicknameMatches => await NoNicknameMatches(ct).ConfigureAwait(false),
            GreetingComponentKeys.SetGuessedNickname => await SetGuessedNickname(dataFragment, ct).ConfigureAwait(false),
            _ => Result.FromSuccess()
        };
    }

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
            await _feedbackService.SendContextualInfoAsync("Hold it, bud. You can't do that!", ct: ct).ConfigureAwait(false);
            return Result.FromSuccess();
        }

        await _guildApi.ModifyGuildMemberAsync(guildID, _context.User.ID, fragmentComponents[1], ct: ct).ConfigureAwait(false);

        IResult alertResponse = await _feedbackService.SendContextualSuccessAsync
        (
            $"Your nickname has been updated to { Formatter.Bold(fragmentComponents[1]) }!",
            ct: ct
        ).ConfigureAwait(false);

        return alertResponse.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(alertResponse.Error!);
    }

    private async Task<Result> SetAlternateRoles(CancellationToken ct = default)
    {
        if (!_context.GuildID.HasValue)
            return Result.FromSuccess();

        if (_context.Data.CustomID.Value is null)
            return Result.FromSuccess();

        // Get the welcome message settings
        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, ct).ConfigureAwait(false);

        ComponentIdFormatter.Parse(_context.Data.CustomID.Value, out ComponentAction _, out string payload);
        ulong userId = ulong.Parse(payload);

        // Check that the user who clicked the button is the focus of the welcome message
        if (_context.User.ID.Value != userId)
        {
            await _replyService.RespondWithUserErrorAsync("Hold it, bud. You can't do that!", ct).ConfigureAwait(false);
            return Result.FromSuccess();
        }

        // Remove the default roles and add the alternate roles
        Result roleChangeResult = await _guildApi.ModifyRoles
        (
            _context.GuildID.Value,
            _context.User.ID,
            _context.Member.Value?.Roles.ToList(),
            welcomeMessage.AlternateRoles,
            welcomeMessage.DefaultRoles,
            ct
        ).ConfigureAwait(false);

        if (!roleChangeResult.IsSuccess)
        {
            _logger.LogError("Failed to modify member roles: {error}", roleChangeResult.Error);
            return roleChangeResult;
        }

        // Inform the user of their role change
        string rolesStringList = string.Join(' ', welcomeMessage.AlternateRoles.Select(r => Formatter.RoleMention(r)));
        Result<IMessage> alertResponse = await _replyService.RespondWithSuccessAsync(
            $"You've been given the following roles: { rolesStringList }",
            ct,
            new AllowedMentions()).ConfigureAwait(false);

        return alertResponse.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(alertResponse);
    }
}
