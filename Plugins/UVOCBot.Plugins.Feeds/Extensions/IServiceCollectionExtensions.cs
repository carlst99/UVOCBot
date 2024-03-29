﻿using Mandible.Abstractions.Manifest;
using Mandible.Manifest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using System;
using Tweetinvi;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Feeds;
using UVOCBot.Plugins.Feeds.Commands;
using UVOCBot.Plugins.Feeds.Responders;
using UVOCBot.Plugins.Feeds.Workers;

// ReSharper disable once CheckNamespace
namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddFeedsPlugin(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<FeedsPluginOptions>(config.GetSection(nameof(FeedsPluginOptions)));

        services.AddTransient(TwitterClientFactory);
        services.AddHttpClient<IManifestService, ManifestService>();

        services.AddComponentResponder<ToggleFeedComponentResponder>(FeedComponentKeys.ToggleFeed);

        services.AddCommandTree()
                .WithCommandGroup<FeedCommands>()
                .Finish();

        services.AddHostedService<ForumRssWorker>()
            .AddHostedService<PatchManifestWorker>()
            .AddHostedService<TwitterWorker>();

        return services;
    }

    private static ITwitterClient TwitterClientFactory(IServiceProvider services)
    {
        FeedsPluginOptions options = services.GetRequiredService<IOptions<FeedsPluginOptions>>().Value;

        return new TwitterClient(options.TwitterKey, options.TwitterSecret, options.TwitterBearerToken);
    }
}
