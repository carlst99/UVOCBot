using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Feeds;
using UVOCBot.Plugins.Feeds.Commands;
using UVOCBot.Plugins.Feeds.Responders;

namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddFeedsPlugin(this IServiceCollection services)
    {
        services.AddComponentResponder<ToggleFeedComponentResponder>(FeedComponentKeys.ToggleFeed);

        services.AddCommandTree()
                .WithCommandGroup<FeedCommands>()
                .Finish();

        return services;
    }
}
