using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Voice.Extensions;
using UVOCBot.Plugins.Music.Abstractions.Services;
using UVOCBot.Plugins.Music.Commands;
using UVOCBot.Plugins.Music.MusicService;
using YoutubeExplode;

namespace UVOCBot.Plugins
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMusicPlugin(this IServiceCollection services)
        {
            services.AddDiscordVoice();

            services.AddCommandGroup<MusicCommands>();

            services.AddScoped<YoutubeClient>();
            services.AddScoped<IYouTubeService, YouTubeService>();
            services.AddSingleton<IMusicService, MusicService>();

            return services;
        }
    }
}
