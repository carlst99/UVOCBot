using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Results;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBotRemora.Workers
{
    public sealed class DiscordWorker : BackgroundService
    {
        private readonly DiscordGatewayClient _discordClient;
        private readonly ILogger<DiscordWorker> _logger;

        public DiscordWorker(
            DiscordGatewayClient discordClient,
            ILogger<DiscordWorker> logger)
        {
            _discordClient = discordClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Result result = await _discordClient.RunAsync(stoppingToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case ExceptionError exe:
                    {
                        _logger.LogCritical(exe.Exception, "Exception during gateway connection: {ExceptionMessage}", exe.Message);
                        break;
                    }
                    case GatewayWebSocketError:
                    case GatewayDiscordError:
                        _logger.LogError("Gateway error: {Message}", result.Unwrap().Message);
                        break;
                    default:
                        _logger.LogError("Unknown error: {Message}", result.Unwrap().Message);
                        break;
                }
            }
        }
    }
}
