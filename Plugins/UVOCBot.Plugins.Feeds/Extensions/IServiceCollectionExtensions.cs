using Mandible.Abstractions.Manifest;
using Mandible.Manifest;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Feeds;
using UVOCBot.Plugins.Feeds.Commands;
using UVOCBot.Plugins.Feeds.Responders;
using UVOCBot.Plugins.Feeds.Workers;

// ReSharper disable once CheckNamespace
namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddFeedsPlugin(this IServiceCollection services)
    {
        services.AddHttpClient<IManifestService, ManifestService>();

        services.AddComponentResponder<ToggleFeedComponentResponder>(FeedComponentKeys.ToggleFeed);

        services.AddCommandTree()
                .WithCommandGroup<FeedCommands>()
                .Finish();

        services.AddHostedService<ForumRssWorker>()
            .AddHostedService<PatchManifestWorker>();

        return services;
    }
}
