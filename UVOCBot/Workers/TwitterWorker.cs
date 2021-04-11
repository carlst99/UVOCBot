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
using UVOCBot.Core.Model;
using UVOCBot.Model;
using UVOCBot.Services;
using UVOCBot.Utilities;

namespace UVOCBot.Workers
{
    public sealed class TwitterWorker : BackgroundService
    {
        private readonly IDiscordRestChannelAPI _discordChannelClient;
        private readonly IDiscordRestGuildAPI _discordGuildClient;
        private readonly ITwitterClient _twitterClient;
        private readonly IAPIService _dbApi;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<TwitterWorker> _logger;

        private readonly MaxSizeQueue<long> _previousTweetIds = new(500);

        public TwitterWorker(
            IDiscordRestChannelAPI discordChannelClient,
            IDiscordRestGuildAPI discordGuildClient,
            ITwitterClient twitterClient,
            IAPIService dbApi,
            ISettingsService settingsService,
            ILogger<TwitterWorker> logger)
        {
            _discordChannelClient = discordChannelClient;
            _discordGuildClient = discordGuildClient;
            _twitterClient = twitterClient;
            _dbApi = dbApi;
            _settingsService = settingsService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Getting tweets");

                Optional<BotSettings> botSettings = await _settingsService.LoadSettings<BotSettings>().ConfigureAwait(false);
                if (!botSettings.HasValue)
                {
                    _logger.LogError("Could not load bot settings");
                    return;
                }

                Dictionary<TwitterUserDTO, List<ITweet>> userTweetPairs = new();
                DateTimeOffset lastFetch = botSettings.Value.TimeOfLastTweetFetch;

                int tweetCount = 0;
                int failureCount = 0;

                // Load all of the twitter users we should relay tweets from
                foreach (GuildTwitterSettingsDTO settings in await _dbApi.GetAllGuildTwitterSettings(true).ConfigureAwait(false))
                {
                    foreach (long twitterUserId in settings.TwitterUsers)
                    {
                        TwitterUserDTO twitterUser = await _dbApi.GetTwitterUser(twitterUserId).ConfigureAwait(false);
                        if (userTweetPairs.ContainsKey(twitterUser))
                        {
                            await PostTweetsToChannelAsync(settings, userTweetPairs[twitterUser], stoppingToken).ConfigureAwait(false);
                        }
                        else
                        {
                            Optional<List<ITweet>> userTweets = await GetUserTweetsAsync(twitterUser, lastFetch).ConfigureAwait(false);
                            if (!userTweets.HasValue)
                            {
                                failureCount++;
                                if (failureCount == 3)
                                    break;

                                continue;
                            } else
                            {
                                // We've changed the last fetched tweet ID, so update the database
                                await _dbApi.UpdateTwitterUser(twitterUserId, twitterUser).ConfigureAwait(false);
                            }

                            tweetCount += userTweets.Value.Count;
                            userTweetPairs.Add(twitterUser, userTweets.Value);

                            await PostTweetsToChannelAsync(settings, userTweets.Value, stoppingToken).ConfigureAwait(false);
                        }
                    }

                    if (failureCount == 3)
                        break;
                }

                botSettings.Value.TimeOfLastTweetFetch = DateTimeOffset.Now;
                await _settingsService.SaveSettings(botSettings.Value).ConfigureAwait(false);
                failureCount = 0;
                _logger.LogInformation($"Finished getting {tweetCount} tweets");

#if DEBUG
                await Task.Delay(30000, stoppingToken).ConfigureAwait(false);
#else
                await Task.Delay(900000, stoppingToken).ConfigureAwait(false);
#endif
            }
        }

        /// <summary>
        /// Gets tweets from a twitter user made after the specified fetch time
        /// </summary>
        /// <param name="user">The twitter user to get tweets from</param>
        /// <param name="lastFetch">The earliest time to fetch tweets from</param>
        /// <returns></returns>
        private async Task<Optional<List<ITweet>>> GetUserTweetsAsync(TwitterUserDTO user, DateTimeOffset lastFetch)
        {
            GetUserTimelineParameters timelineParameters = new(user.UserId)
            {
                ExcludeReplies = true,
                IncludeRetweets = true,
                PageSize = 25,
                SinceId = user.LastRelayedTweetId
            };

            ITweet[] tweets;
            try
            {
                // TODO: Determine if a twitter user has been deleted
                tweets = await _twitterClient.Timelines.GetUserTimelineAsync(timelineParameters).ConfigureAwait(false);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get user timeline");
                return Optional<List<ITweet>>.FromNoValue();
            }

            if (tweets.Length == 0)
                return Optional<List<ITweet>>.FromNoValue();

            user.LastRelayedTweetId = tweets[0].Id;
            Array.Reverse(tweets);

            List<ITweet> validTweets = new();
            foreach (ITweet tweet in tweets)
            {
                // TODO: Investigate if we can instead determine posting time by the tweet id. I.e. do the most recent tweets have the highest IDs
                if (tweet.CreatedAt < lastFetch)
                    continue;
                // Following for testing purposes only
                //if (tweet.CreatedAt < DateTimeOffset.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0)))
                    //continue;

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
            return Optional<List<ITweet>>.FromValue(validTweets);
        }

        private async Task PostTweetsToChannelAsync(GuildTwitterSettingsDTO settings, List<ITweet> tweets, CancellationToken stoppingToken)
        {
            Result<IGuild> guild = await _discordGuildClient.GetGuildAsync(new Remora.Discord.Core.Snowflake(settings.GuildId), ct: stoppingToken).ConfigureAwait(false);
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

            Result<IChannel> channel = await _discordChannelClient.GetChannelAsync(channelSnowflake, stoppingToken).ConfigureAwait(false);
            if (!channel.IsSuccess)
            {
                Result<IReadOnlyList<IChannel>> channels = await _discordGuildClient.GetGuildChannelsAsync(new Remora.Discord.Core.Snowflake(settings.GuildId), stoppingToken).ConfigureAwait(false);
                if (!channels.IsSuccess)
                    return;

                // TODO: Find first channel that we have permission to send to
                // "The tweet relay channel could not be found. Please reset it."
            }

            try
            {
                foreach (ITweet tweet in tweets)
                    await _discordChannelClient.CreateMessageAsync(channelSnowflake, content: tweet.Url, ct: stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send tweet");
            }
        }
    }
}
