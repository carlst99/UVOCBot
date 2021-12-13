using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Greetings.Abstractions.Services;

namespace UVOCBot.Plugins.Greetings.Responders;

/// <summary>
/// Represents a responder for sending a greeting when an <see cref="IGuildMemberAdd"/> event occurs.
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
    {
        IResult res = await _greetingService.SendGreeting(gatewayEvent.GuildID, gatewayEvent, ct).ConfigureAwait(false);

        return !res.IsSuccess
            ? Result.FromError(res.Error!)
            : Result.FromSuccess();
    }
}
