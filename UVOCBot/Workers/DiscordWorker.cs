using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;

namespace UVOCBot.Workers
{
    public sealed class DiscordWorker : BackgroundService
    {
        private readonly DiscordClient _discordClient;
        private readonly GeneralOptions _generalOptions;

        public DiscordWorker(
            DiscordClient discordClient,
            IOptions<GeneralOptions> generalOptions)
        {
            _discordClient = discordClient;
            _generalOptions = generalOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discordClient.ConnectAsync(new DiscordActivity(_generalOptions.CommandPrefix + "help", ActivityType.ListeningTo)).ConfigureAwait(false);
            await Task.Delay(-1, stoppingToken).ConfigureAwait(false);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordClient.DisconnectAsync().ConfigureAwait(false);
        }
    }
}
