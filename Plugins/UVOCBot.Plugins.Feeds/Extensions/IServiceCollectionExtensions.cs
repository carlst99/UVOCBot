using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Feeds;
using UVOCBot.Plugins.Feeds.Commands;
using UVOCBot.Plugins.Feeds.Responders;
using UVOCBot.Plugins.Feeds.Workers;

namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddFeedsPlugin(this IServiceCollection services)
    {
        services.AddComponentResponder<ToggleFeedComponentResponder>(FeedComponentKeys.ToggleFeed);

        services.AddCommandTree()
                .WithCommandGroup<FeedCommands>()
                .Finish();

        services.AddHostedService<TwitterWorker>();

        return services;
    }
}
