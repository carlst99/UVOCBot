using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Greetings.Abstractions.Services;

namespace UVOCBot.Plugins.Greetings.Responders;

/// <summary>
/// Responsible for sending a greeting message when a new member joins a guild.
/// </summary>
internal sealed class GuildMemberAddGreetingResponder : IResponder<IGuildMemberAdd>
{
    private readonly IGreetingService _greetingService;

    public GuildMemberAddGreetingResponder(IGreetingService greetingService)
    {
        _greetingService = greetingService;
    }

    /// <inheritdoc />
    public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        => await _greetingService.SendGreeting(gatewayEvent.GuildID, gatewayEvent, ct).ConfigureAwait(false);
}
