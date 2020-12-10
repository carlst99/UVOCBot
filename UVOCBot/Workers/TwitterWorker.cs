using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using System.Linq;
using Tweetinvi.Models;
using UVOCBot.Model;
using UVOCBot.Utils;
using Tweetinvi.Parameters;

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

                // Update our time record
#if DEBUG
                // Give us six hours of play when debugging
                if (db.ActualBotSettings.TimeOfLastTwitterFetch.AddHours(6) < DateTimeOffset.UtcNow)
                    db.ActualBotSettings.TimeOfLastTwitterFetch = DateTimeOffset.UtcNow;
#else
                db.ActualBotSettings.TimeOfLastTwitterFetch = DateTimeOffset.UtcNow;
#endif
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
                tweets = await _twitterClient.Timelines.GetUserTimelineAsync(timelineParameters).ConfigureAwait(false);
            } catch (Exception ex)
            {
                Log.Error(ex, "Couldn't get user timeline");
                return null;
            }

            if (tweets.Length > 0)
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
            try
            {
                foreach (ITweet tweet in tweets)
                    await SendMessageAsync(settings, tweet.Url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[{nameof(TwitterWorker)}] Could not send tweet");
            }
        }

        private async Task SendMessageAsync(GuildTwitterSettings settings, string message)
        {
            DiscordChannel channel;
            try
            {
                if (settings.RelayChannelId is null)
                    throw new Exception();
                channel = await _discordClient.GetChannelAsync((ulong)settings.RelayChannelId).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                channel = (await _discordClient.GetGuildAsync(settings.GuildId).ConfigureAwait(false)).GetDefaultChannel();
                await channel.SendMessageAsync($":warning: {Program.NAME} can't find the Twitter relay channel. Has it been deleted? Please reset it using the `{Program.PREFIX}twitter relay-channel`").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[{nameof(TwitterWorker)}] Could not get channel to send tweets to");
                return;
            }

            await channel.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
