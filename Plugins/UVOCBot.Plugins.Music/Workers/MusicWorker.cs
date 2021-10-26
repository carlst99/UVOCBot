using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Music.Abstractions.Services;

namespace UVOCBot.Plugins.Music.Workers
{
    internal sealed class MusicWorker : BackgroundService
    {
        private readonly IMusicService _musicService;

        public MusicWorker(IMusicService musicService)
        {
            _musicService = musicService;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await _musicService.RunAsync(ct).ConfigureAwait(false);
        }
    }
}
