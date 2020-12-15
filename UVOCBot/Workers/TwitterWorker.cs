using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using UVOCBot.Model;
using UVOCBot.Utils;

namespace UVOCBot.Workers
{
    public sealed class TwitterWorker : BackgroundService
    {
        private readonly DiscordClient _discordClient;
        private readonly ITwitterClient _twitterClient;

        private readonly MaxSizeQueue<long> _previousTweetIds = new MaxSizeQueue<long>(100);

        public TwitterWorker(
            DiscordClient discordClient,
            ITwitterClient twitterClient)
        {
            _discordClient = discordClient;
            _twitterClient = twitterClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Log.Debug($"[{nameof(TwitterWorker)}] Getting tweets");

                Dictionary<TwitterUser, List<ITweet>> userTweetPairs = new Dictionary<TwitterUser, List<ITweet>>();
                using BotContext db = new BotContext();
                DateTimeOffset lastFetch = db.ActualBotSettings.TimeOfLastTwitterFetch;

                int tweetCount = 0;
                int failureCount = 0;

                // Load all of the twitter users we should relay tweets from
                foreach (GuildTwitterSettings settings in db.GuildTwitterSettings.Include(e => e.TwitterUsers).Where(s => s.IsEnabled))
                {
                    foreach (TwitterUser user in settings.TwitterUsers)
                    {
                        if (userTweetPairs.ContainsKey(user))
                        {
                            await PostTweetsToChannelAsync(settings, userTweetPairs[user]).ConfigureAwait(false);
                        }
                        else
                        {
                            List<ITweet> userTweets = await GetUserTweetsAsync(user, lastFetch).ConfigureAwait(false);
                            if (userTweets is null)
                            {
                                failureCount++;
                                if (failureCount == 3)
                                    return;

                                continue;
                            }

                            tweetCount += userTweets.Count;
                            userTweetPairs.Add(user, userTweets);

                            await PostTweetsToChannelAsync(settings, userTweets).ConfigureAwait(false);
                        }
                    }
                }

                db.ActualBotSettings.TimeOfLastTwitterFetch = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

                Log.Information($"[{nameof(TwitterWorker)}] Finished getting {tweetCount} tweets");

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
        /// <param name="userId">The ID of the twitter user to get tweets from</param>
        /// <param name="lastFetch">The earliest time to fetch tweets from</param>
        /// <returns></returns>
        private async Task<List<ITweet>> GetUserTweetsAsync(TwitterUser user, DateTimeOffset lastFetch)
        {
            GetUserTimelineParameters timelineParameters = new GetUserTimelineParameters(user.UserId)
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
                Log.Error(ex, "Couldn't get user timeline");
                return null;
            }

            if (tweets.Length == 0)
                return null;

            user.LastRelayedTweetId = tweets[0].Id;
            Array.Reverse(tweets);

            List<ITweet> validTweets = new List<ITweet>();
            foreach (ITweet tweet in tweets)
            {
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
            return validTweets;
        }

        private async Task PostTweetsToChannelAsync(GuildTwitterSettings settings, List<ITweet> tweets)
        {
            ChannelReturnedInfo channelInfo = DiscordClientUtils.TryGetGuildChannel(_discordClient, settings.GuildId, settings.RelayChannelId);

            switch (channelInfo.Status)
            {
                case ChannelReturnedInfo.GetChannelStatus.Failure:
                    return;
                case ChannelReturnedInfo.GetChannelStatus.GuildNotFound:
                    return; // TODO: Do something if the guild was not found
                case ChannelReturnedInfo.GetChannelStatus.Fallback:
                    await channelInfo.Channel.SendMessageAsync($"The tweet relay channel could not be found. Please reset it using the `{Program.PREFIX}twitter relay-channel` command").ConfigureAwait(false);
                    break;
            }

            try
            {
                foreach (ITweet tweet in tweets)
                    await channelInfo.Channel.SendMessageAsync(tweet.Url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[{nameof(TwitterWorker)}] Could not send tweet");
            }
        }
    }
}
