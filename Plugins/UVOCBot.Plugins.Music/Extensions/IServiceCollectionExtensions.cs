using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Voice.Extensions;
using UVOCBot.Plugins.Music.Abstractions.Services;
using UVOCBot.Plugins.Music.Commands;
using UVOCBot.Plugins.Music.MusicService;
using UVOCBot.Plugins.Music.Workers;
using YoutubeExplode;

namespace UVOCBot.Plugins
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMusicPlugin(this IServiceCollection services)
        {
            services.AddDiscordVoice();

            services.AddCommandGroup<MusicCommands>();

            services.AddSingleton<YoutubeClient>();
            services.AddSingleton<IYouTubeService, YouTubeService>();
            services.AddSingleton<IMusicService, MusicService>();

            services.AddHostedService<MusicWorker>();

            return services;
        }
    }
}
