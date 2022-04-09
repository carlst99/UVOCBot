using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Discord.Core;

namespace UVOCBot.Responders;

/// <summary>
/// Performs setup actions once a connection has been established to the Discord gateway
/// </summary>
public class ReadyResponder : IResponder<IReady>
{
    private readonly ILogger<ReadyResponder> _logger;
    private readonly GeneralOptions _options;
    private readonly DiscordGatewayClient _client;

    public ReadyResponder
    (
        ILogger<ReadyResponder> logger,
        IOptions<GeneralOptions> options,
        DiscordGatewayClient client
    )
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
    }

    public Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
    {
        DiscordConstants.UserId = gatewayEvent.User.ID;

        if (gatewayEvent.Application.ID.HasValue)
        {
            DiscordConstants.ApplicationId = gatewayEvent.Application.ID.Value;
        }

        _client.SubmitCommand
        (
            new UpdatePresence
            (
                ClientStatus.Online,
                false,
                null,
                new Activity[] { new (_options.DiscordPresence, ActivityType.Game) }
            )
        );

        _logger.LogInformation("Ready!");

        return Task.FromResult(Result.FromSuccess());
    }
}
