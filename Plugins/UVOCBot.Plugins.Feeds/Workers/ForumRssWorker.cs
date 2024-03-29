﻿using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using Feed = UVOCBot.Plugins.Feeds.Objects.Feed;

namespace UVOCBot.Plugins.Feeds.Workers;

public sealed class ForumRssWorker : BackgroundService
{
    /// <summary>
    /// Gets a mapping of feeds to forum RSS feeds.
    /// </summary>
    private static readonly IReadOnlyDictionary<Feed, string> ForumRssFeeds = new Dictionary<Feed, string>()
    {
        { Feed.ForumAnnouncement, "https://forums.daybreakgames.com/ps2/index.php?forums/official-news-and-announcements.19/index.rss" },
        { Feed.ForumPatchNotes, "https://forums.daybreakgames.com/ps2/index.php?forums/game-update-notes.73/index.rss" },
        { Feed.ForumPTSAnnouncement, "https://forums.daybreakgames.com/ps2/index.php?forums/test-server-announcements.69/index.rss" },
        { Feed.News, "https://www.planetside2.com/rss" }
    };

    private readonly ILogger<ForumRssWorker> _logger;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;

    private readonly Dictionary<Feed, DateTime> _lastRelayedPosts;

    public ForumRssWorker
    (
        ILogger<ForumRssWorker> logger,
        IDiscordRestChannelAPI channelApi,
        IDbContextFactory<DiscordContext> dbContextFactory
    )
    {
        _logger = logger;
        _channelApi = channelApi;
        _dbContextFactory = dbContextFactory;

        _lastRelayedPosts = new Dictionary<Feed, DateTime>();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await InitializeAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            Dictionary<Feed, FeedBundle> unseenPosts = new();
            foreach (Feed feed in ForumRssFeeds.Keys)
                unseenPosts[feed] = await GetUnseenPostsAsync(feed, ct);

            DiscordContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            foreach (GuildFeedsSettings feedsSettings in dbContext.GuildFeedsSettings)
            {
                foreach (Feed feed in unseenPosts.Keys)
                {
                    if (((Feed)feedsSettings.Feeds & feed) == 0)
                        continue;

                    await PostItemsToChannelAsync(feedsSettings, unseenPosts[feed], ct);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        foreach (Feed fType in ForumRssFeeds.Keys)
        {
            CodeHollow.FeedReader.Feed? feed = await TryGetFeedAsync(fType, ct);
            if (feed is null || feed.Items.Count == 0)
                continue;

            if (feed.Items.Count is 0)
                _lastRelayedPosts[fType] = DateTime.MinValue;
            else
                _lastRelayedPosts[fType] = GetItemPublishingDate(feed.Items[0]);
        }
    }

    private async Task<FeedBundle> GetUnseenPostsAsync(Feed fType, CancellationToken ct)
    {
        List<FeedItem> validItems = new();

        CodeHollow.FeedReader.Feed? feed = await TryGetFeedAsync(fType, ct);
        if (feed is null || feed.Items.Count == 0)
            return new FeedBundle(feed?.Title, validItems);

        foreach (FeedItem item in feed.Items)
        {
            if (GetItemPublishingDate(item) > _lastRelayedPosts[fType])
                validItems.Add(item);
        }

        if (validItems.Count > 0)
        {
            validItems.Sort
            (
                (x, y) => GetItemPublishingDate(x).CompareTo(GetItemPublishingDate(y))
            );
            _lastRelayedPosts[fType] = GetItemPublishingDate(validItems[0]);
        }

        return new FeedBundle(feed.Title, validItems);
    }

    private async Task PostItemsToChannelAsync(GuildFeedsSettings settings, FeedBundle bundle, CancellationToken ct)
    {
        IReadOnlyList<FeedItem> posts = bundle.Posts;
        if (posts.Count == 0)
            return;

        if (!settings.IsEnabled || settings.FeedChannelID is null)
            return;

        Snowflake channelID = DiscordSnowflake.New(settings.FeedChannelID.Value);

        foreach (FeedItem item in posts)
        {
            bool couldParseDate = DateTimeOffset.TryParse(item.PublishingDateString, out DateTimeOffset pubDate);
            bool couldFindImage = TryFindFirstImageLink(item.Description, out string? imageUrl);

            Embed testEmbed = new
            (
                item.Title,
                Description: $"{item.Link}\n\n{item.Description.RemoveHtml(200)}...",
                Url: item.Link,
                Image: couldFindImage ? new EmbedImage(imageUrl!) : new Optional<IEmbedImage>(),
                Author: new EmbedAuthor(item.Author),
                Timestamp: couldParseDate ? pubDate : default,
                Footer: bundle.ChannelName is null
                    ? default(Optional<IEmbedFooter>)
                    : new EmbedFooter(bundle.ChannelName)
            );

            await _channelApi.CreateMessageAsync
            (
                channelID,
                embeds: new[] { testEmbed },
                ct: ct
            );
        }
    }

    private async Task<CodeHollow.FeedReader.Feed?> TryGetFeedAsync(Feed fType, CancellationToken ct)
    {
        CodeHollow.FeedReader.Feed? feed = null;
        try
        {
            feed = await FeedReader.ReadAsync(ForumRssFeeds[fType], ct)
                .WithCancellation(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not retrieve RSS feed");
        }

        return feed;
    }

    private static bool TryFindFirstImageLink(string html, [NotNullWhen(true)] out string? imgLink)
    {
        imgLink = null;

        int imgElementIndex = html.IndexOf("<img", StringComparison.OrdinalIgnoreCase);
        if (imgElementIndex < 0)
            return false;

        int srcAttributeIndex = html.IndexOf("src=", StringComparison.OrdinalIgnoreCase);
        if (srcAttributeIndex < 0)
            return false;

        int attrStartIndex = html.IndexOf('"', srcAttributeIndex);
        if (attrStartIndex < 0)
            return false;
        attrStartIndex++;

        int attrEndIndex = html.IndexOf('"', attrStartIndex);
        if (attrEndIndex < 0)
            return false;

        imgLink = html.Substring(attrStartIndex, attrEndIndex - attrStartIndex);
        return true;
    }

    private static DateTime GetItemPublishingDate(FeedItem item)
        => item.PublishingDate ?? DateTime.Now;

    private record FeedBundle(string? ChannelName, IReadOnlyList<FeedItem> Posts);
}
