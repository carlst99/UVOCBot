using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Collections.Generic;
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
    private readonly IHostApplicationLifetime _appLifetime;

    public ReadyResponder
    (
        ILogger<ReadyResponder> logger,
        IOptions<GeneralOptions> options,
        DiscordGatewayClient client,
        IHostApplicationLifetime appLifetime
    )
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _appLifetime = appLifetime;
    }

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
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
                Activities: new Activity[] { new Activity(_options.DiscordPresence, ActivityType.Game) }
            )
        );

        await PrepareDatabase(gatewayEvent.Guilds, ct).ConfigureAwait(false);
        _logger.LogInformation("Ready!");

        return Result.FromSuccess();
    }

    private async Task PrepareDatabase(IReadOnlyList<IUnavailableGuild> guilds, CancellationToken ct = default)
    {
        // TODO: Do we need to do any DB scaffolding here?
    }
}
