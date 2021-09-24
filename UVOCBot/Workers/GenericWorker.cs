using DbgCensus.EventStream.Abstractions;
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

namespace UVOCBot.Workers
{
    public class GenericWorker : BackgroundService
    {
        private readonly ILogger<GenericWorker> _logger;
        private readonly GeneralOptions _generalOptions;
        private readonly DiscordGatewayClient _gatewayClient;
        private readonly IEventStreamClientFactory _eventStreamClientFactory;

        public GenericWorker(
            ILogger<GenericWorker> logger,
            IOptions<GeneralOptions> generalOptions,
            DiscordGatewayClient gatewayClient,
            IEventStreamClientFactory eventStreamClientFactory)
        {
            _logger = logger;
            _generalOptions = generalOptions.Value;
            _gatewayClient = gatewayClient;
            _eventStreamClientFactory = eventStreamClientFactory;
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
                        _gatewayClient.SubmitCommandAsync(
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

            // Every 15m
            Task fifteenMin = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        /* Resubscribe to our census events */
                        IEventStreamClient client = _eventStreamClientFactory.GetClient(BotConstants.CENSUS_EVENTSTREAM_CLIENT_NAME);
                        if (client.IsRunning)
                            await client.SendCommandAsync(BotConstants.CORE_CENSUS_SUBSCRIPTION, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not TaskCanceledException)
                    {
                        _logger.LogError(ex, "Failed to run 15m tasks.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
                }
            }, ct);

            await Task.WhenAll(hourly, fifteenMin).ConfigureAwait(false);
        }
    }
}
