using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services;

namespace UVOCBot.Workers
{
    public sealed class DiscordWorker : BackgroundService
    {
        private readonly DiscordClient _discordClient;

        public DiscordWorker(
            DiscordClient discordClient)
        {
            _discordClient = discordClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discordClient.ConnectAsync(new DiscordActivity(IPrefixService.DEFAULT_PREFIX + "help", ActivityType.ListeningTo)).ConfigureAwait(false);
            await Task.Delay(-1, stoppingToken).ConfigureAwait(false);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordClient.DisconnectAsync().ConfigureAwait(false);
        }
    }
}
