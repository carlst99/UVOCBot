using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using UVOCBot.Core.Dto;
using UVOCBot.Services.Abstractions;
using UVOCBot.Utilities;

namespace UVOCBot.Workers
{
    public sealed class TwitterWorker : BackgroundService
    {
        private readonly IDiscordRestChannelAPI _discordChannelClient;
        private readonly IDiscordRestGuildAPI _discordGuildClient;
        private readonly ITwitterClient _twitterClient;
        private readonly IDbApiService _dbApi;
        private readonly ILogger<TwitterWorker> _logger;

        private readonly MaxSizeQueue<long> _previousTweetIds = new(500);

        public TwitterWorker(
            IDiscordRestChannelAPI discordChannelClient,
            IDiscordRestGuildAPI discordGuildClient,
            ITwitterClient twitterClient,
            IDbApiService dbApi,
            ILogger<TwitterWorker> logger)
        {
            _discordChannelClient = discordChannelClient;
            _discordGuildClient = discordGuildClient;
            _twitterClient = twitterClient;
            _dbApi = dbApi;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogTrace("Getting tweets.");

                Dictionary<long, List<ITweet>> userTweetPairs = new();

                int tweetCount = 0;

                // Get all the guilds that have tweet relaying enabled
                Result<List<GuildTwitterSettingsDto>> settingsResult = await _dbApi.ListGuildTwitterSettingsAsync(true, stoppingToken).ConfigureAwait(false);                if (!settingsResult.IsSuccess)
                {
                    _logger.LogError("Failed to load guild twitter settings: {ex}", settingsResult.Error);
                    return;
                }

                foreach (GuildTwitterSettingsDto settings in settingsResult.Entity)
                {
                    foreach (long twitterUserId in settings.TwitterUsers)
                    {
                        // Get tweets from this user if we haven't already
                        if (!userTweetPairs.ContainsKey(twitterUserId))
                        {
                            Result<TwitterUserDto> dbTwitterUserResult = await _dbApi.GetTwitterUserAsync(twitterUserId, stoppingToken).ConfigureAwait(false);
                            if (!dbTwitterUserResult.IsSuccess)
                            {
                                _logger.LogError("Failed to get twitter user from database: {ex}", dbTwitterUserResult.Error);
                                continue;
                            }

                            List<ITweet> userTweets;
                            try
                            {
                                userTweets = await GetUserTweetsAsync(dbTwitterUserResult.Entity, stoppingToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to get tweets: {ex}");
                                continue;
                            }

                            tweetCount += userTweets.Count;
                            userTweetPairs.Add(twitterUserId, userTweets);
                        }

                        await PostTweetsToChannelAsync(settings, userTweetPairs[twitterUserId], stoppingToken).ConfigureAwait(false);
                    }
                }

                _logger.LogTrace($"Finished getting {tweetCount} tweets");

#if DEBUG
                await Task.Delay(30000, stoppingToken).ConfigureAwait(false);
#else
                await Task.Delay(900000, stoppingToken).ConfigureAwait(false);
#endif
            }
        }

        /// <summary>
        /// Gets tweets from a twitter user made after the last tweets we fetched from them.
        /// </summary>
        /// <param name="user">The twitter user to get tweets from.</param>
        /// <param name="ct">A token with which to cancel any asynchronous operations.</param>
        /// <returns></returns>
        private async Task<List<ITweet>> GetUserTweetsAsync(TwitterUserDto user, CancellationToken ct)
        {
            GetUserTimelineParameters timelineParameters = new(user.UserId)
            {
                ExcludeReplies = true,
                IncludeRetweets = true,
                PageSize = 25,
                SinceId = user.LastRelayedTweetId
            };

            ITweet[]? tweets = null;
            try
            {
                // TODO: Determine if a twitter user has been deleted
                tweets = await _twitterClient.Timelines.GetUserTimelineAsync(timelineParameters).WithCancellation(ct).ConfigureAwait(false);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get user timeline");
            }

            if (tweets is null || tweets.Length == 0)
                return new List<ITweet>();

            // We've changed the last fetched tweet ID, so update the database
            user.LastRelayedTweetId = tweets[0].Id;
            await _dbApi.UpdateTwitterUserAsync(user.UserId, user, ct).ConfigureAwait(false);

            List<ITweet> validTweets = new();
            foreach (ITweet tweet in tweets)
            {
                // Filter out retweets of tweets that we have already posted
                long tweetId;
                if (tweet.IsRetweet)
                    tweetId = tweet.RetweetedTweet.Id;
                else
                    tweetId = tweet.Id;

                if (!_previousTweetIds.Contains(tweetId))
                {
                    validTweets.Add(tweet);
                    _previousTweetIds.Enqueue(tweetId);
                }
            }

            validTweets.Sort((x, y) => x.CreatedAt.CompareTo(y.CreatedAt));
            return validTweets;
        }

        private async Task PostTweetsToChannelAsync(GuildTwitterSettingsDto settings, List<ITweet> tweets, CancellationToken ct)
        {
            if (tweets.Count == 0)
                return;

            Result<IGuild> guild = await _discordGuildClient.GetGuildAsync(new Remora.Discord.Core.Snowflake(settings.GuildId), ct: ct).ConfigureAwait(false);
            if (!guild.IsSuccess)
            {
                // TODO: Do something if the guild was not found
                // Don't just remove it, will have to ensure that the API wasn't playing up first
                // Or perhaps don't worry, and implement some form of cleanup algorithm in the data layer
                return;
            }

            if (settings.RelayChannelId is null)
                return;
            Remora.Discord.Core.Snowflake channelSnowflake = new((ulong)settings.RelayChannelId);

            Result<IChannel> channel = await _discordChannelClient.GetChannelAsync(channelSnowflake, ct).ConfigureAwait(false);
            if (!channel.IsSuccess)
            {
                Result<IReadOnlyList<IChannel>> channels = await _discordGuildClient.GetGuildChannelsAsync(new Remora.Discord.Core.Snowflake(settings.GuildId), ct).ConfigureAwait(false);
                if (!channels.IsSuccess)
                    return;

                // TODO: Find first channel that we have permission to send to
                // "The tweet relay channel could not be found. Please reset it."
            }

            try
            {
                foreach (ITweet tweet in tweets)
                    await _discordChannelClient.CreateMessageAsync(channelSnowflake, content: tweet.Url, ct: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send tweet");
            }
        }
    }
}
