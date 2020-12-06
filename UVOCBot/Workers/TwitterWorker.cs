﻿using DSharpPlus;
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
using Tweetinvi.Models;
using UVOCBot.Model;

namespace UVOCBot.Workers
{
    public sealed class TwitterWorker : BackgroundService
    {
        private readonly DiscordClient _discordClient;
        private readonly ITwitterClient _twitterClient;

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

                Dictionary<long, List<ITweet>> userTweetPairs = new Dictionary<long, List<ITweet>>();
                using BotContext db = new BotContext();
                DateTimeOffset lastFetch = db.ActualBotSettings.TimeOfLastTwitterFetch;
                int tweetCount = 0;

                // Load all of the twitter users we should relay tweets from
                foreach (GuildTwitterSettings settings in db.GuildTwitterSettings.Include(e => e.TwitterUsers))
                {
                    foreach (TwitterUser user in settings.TwitterUsers)
                    {
                        if (userTweetPairs.ContainsKey(user.UserId))
                        {
                            await PostTweetsToChannel(settings, userTweetPairs[user.UserId]).ConfigureAwait(false);
                        }
                        else
                        {
                            List<ITweet> userTweets = await GetUserTweets(user.UserId, lastFetch).ConfigureAwait(false);
                            tweetCount += userTweets.Count;

                            userTweetPairs.Add(user.UserId, userTweets);
                            await PostTweetsToChannel(settings, userTweets).ConfigureAwait(false);
                        }
                    }
                }

                // Update our time record
#if DEBUG
                // Give us six hours of play when debugging
                if (db.ActualBotSettings.TimeOfLastTwitterFetch.AddHours(6) < DateTimeOffset.Now)
                    db.ActualBotSettings.TimeOfLastTwitterFetch = DateTimeOffset.Now;
#else
                db.ActualBotSettings.TimeOfLastTwitterFetch = DateTimeOffset.Now;
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
        private async Task<List<ITweet>> GetUserTweets(long userId, DateTimeOffset lastFetch)
        {
            ITweet[] tweets = await _twitterClient.Timelines.GetUserTimelineAsync(userId).ConfigureAwait(false);

            List<ITweet> validTweets = new List<ITweet>();
            foreach (ITweet tweet in tweets)
            {
                if (tweet.CreatedAt < lastFetch)
                    break;

                // Presumably, all of the devs are being tracked. Therefore, retweets won't reveal any unknown info
                if (tweet.IsRetweet)
                    continue;

                validTweets.Add(tweet);
            }
            return validTweets;
        }

        private async Task PostTweetsToChannel(GuildTwitterSettings settings, List<ITweet> tweets)
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

            try
            {
                foreach (ITweet tweet in tweets)
                    await channel.SendMessageAsync(tweet.Url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[{nameof(TwitterWorker)}] Could not send tweet");
            }
        }
    }
}