using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Core.Util;

namespace UVOCBot.Plugins.Feeds.Workers;

public sealed class TwitterWorker : BackgroundService
{
    /// <summary>
    /// Gets a mapping of feeds to twitter users.
    /// </summary>
    private static readonly IReadOnlyDictionary<Feed, long> UserFeeds = new Dictionary<Feed, long>()
    {
        { Feed.TwitterPlanetside, 247430686 },
        { Feed.TwitterRPG, 1149006335863771136 },
        { Feed.TwitterWrel, 829358606 }
    };

    private readonly ILogger<TwitterWorker> _logger;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly ITwitterClient _twitterClient;
    private readonly IDbContextFactory<DiscordContext> _dbContextFactory;

    private readonly Dictionary<long, long> _lastRelayedTweetIds;
    private readonly BoundedQueue<long> _previouslySeenTweetIds;

    public TwitterWorker
    (
        ILogger<TwitterWorker> logger,
        IDiscordRestChannelAPI channelApi,
        ITwitterClient twitterClient,
        IDbContextFactory<DiscordContext> dbContextFactory
    )
    {
        _logger = logger;
        _channelApi = channelApi;
        _twitterClient = twitterClient;
        _dbContextFactory = dbContextFactory;

        _lastRelayedTweetIds = new Dictionary<long, long>();
        _previouslySeenTweetIds = new BoundedQueue<long>(100);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await InitializeAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            Dictionary<Feed, List<ITweet>> unseenTweets = new();
            foreach (Feed feed in UserFeeds.Keys)
                unseenTweets[feed] = await GetUnseenUserTweetsAsync(UserFeeds[feed], ct);

            DiscordContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            foreach (GuildFeedsSettings feedsSettings in dbContext.GuildFeedsSettings)
            {
                foreach (Feed feed in unseenTweets.Keys)
                {
                    if (((Feed)feedsSettings.Feeds & feed) == 0)
                        continue;

                    await PostTweetsToChannelAsync(feedsSettings, unseenTweets[feed], ct);
                }
            }

#if DEBUG
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
#else
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
#endif
        }
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        foreach (long user in UserFeeds.Values)
        {
            GetUserTimelineParameters timelineParameters = new(user)
            {
                ExcludeReplies = true,
                IncludeRetweets = true,
                PageSize = 1
            };

            ITweet[]? tweets = null;
            try
            {
                tweets = await _twitterClient.Timelines.GetUserTimelineAsync(timelineParameters).WithCancellation(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get user timeline");
            }

            if (tweets is null || tweets.Length == 0)
                continue;

            _lastRelayedTweetIds[user] = tweets[0].Id;
        }
    }

    /// <summary>
    /// Gets tweets from a twitter user made after the last tweets we fetched from them.
    /// </summary>
    /// <param name="userID">The twitter user to get tweets from.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A list of tweets.</returns>
    private async Task<List<ITweet>> GetUnseenUserTweetsAsync(long userID, CancellationToken ct)
    {
        List<ITweet> validTweets = new();

        GetUserTimelineParameters timelineParameters = new(userID)
        {
            ExcludeReplies = true,
            IncludeRetweets = true,
            PageSize = 100
        };

        if (_lastRelayedTweetIds.ContainsKey(userID))
            timelineParameters.SinceId = _lastRelayedTweetIds[userID];

        ITweet[]? tweets = null;
        try
        {
            tweets = await _twitterClient.Timelines.GetUserTimelineAsync(timelineParameters).WithCancellation(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get user timeline");
        }

        if (tweets is null || tweets.Length == 0)
            return validTweets;

        foreach (ITweet tweet in tweets)
        {
            // Filter out retweets of tweets that we have already posted
            long tweetId;
            if (tweet.IsRetweet)
                tweetId = tweet.RetweetedTweet.Id;
            else
                tweetId = tweet.Id;

            if (_previouslySeenTweetIds.Contains(tweetId))
                continue;

            validTweets.Add(tweet);
            _previouslySeenTweetIds.Enqueue(tweetId);
        }

        if (validTweets.Count > 0)
        {
            validTweets.Sort((x, y) => x.CreatedAt.CompareTo(y.CreatedAt));
            _lastRelayedTweetIds[userID] = validTweets[0].Id;
        }

        return validTweets;
    }

    private async Task PostTweetsToChannelAsync(GuildFeedsSettings settings, List<ITweet> tweets, CancellationToken ct)
    {
        if (tweets.Count == 0)
            return;

        if (!settings.IsEnabled || settings.FeedChannelID is null)
            return;

        Snowflake channelID = DiscordSnowflake.New(settings.FeedChannelID.Value);

        foreach (ITweet tweet in tweets)
            await _channelApi.CreateMessageAsync(channelID, content: tweet.Url, ct: ct);
    }
}
