using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;

namespace UVOCBot.Workers;

public class GenericWorker : BackgroundService
{
    private readonly ILogger<GenericWorker> _logger;
    private readonly GeneralOptions _generalOptions;
    private readonly DiscordGatewayClient _gatewayClient;

    public GenericWorker(
        ILogger<GenericWorker> logger,
        IOptions<GeneralOptions> generalOptions,
        DiscordGatewayClient gatewayClient)
    {
        _logger = logger;
        _generalOptions = generalOptions.Value;
        _gatewayClient = gatewayClient;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Hourly
        Task hourly = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                        /* Update our presence */
                    _gatewayClient.SubmitCommand(
                        new UpdatePresence(
                            ClientStatus.Online,
                            false,
                            null,
                            Activities: new Activity[] { new Activity(_generalOptions.DiscordPresence, ActivityType.Game) }
                        )
                    );

                    await Task.Delay(TimeSpan.FromHours(1), ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    _logger.LogError(ex, "Failed to run hourly tasks.");
                }
            }
        }, ct);

        await hourly.ConfigureAwait(false);
    }
}
