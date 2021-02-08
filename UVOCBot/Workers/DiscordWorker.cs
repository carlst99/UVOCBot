using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Workers
{
    public sealed class DiscordWorker : BackgroundService
    {
        private readonly DiscordClient _discordClient;

        public DiscordWorker(
            IHostApplicationLifetime appLifetime,
            DiscordClient discordClient)
        {
            _discordClient = discordClient;
            appLifetime.ApplicationStopping.Register(OnStopping);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discordClient.ConnectAsync(new DiscordActivity(Program.DEFAULT_PREFIX + "help", ActivityType.ListeningTo)).ConfigureAwait(false);
            await Task.Delay(-1, stoppingToken).ConfigureAwait(false);
        }

        private void OnStopping()
        {
            _discordClient.DisconnectAsync().Wait();
        }
    }
}
