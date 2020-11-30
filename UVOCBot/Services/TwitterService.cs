using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using FluentScheduler;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBot.Model;

namespace UVOCBot.Services
{
    public sealed class TwitterService : IJob
    {
        private readonly TwitterClient _client;

        public TwitterService()
        {
            string apiKey = Environment.GetEnvironmentVariable("TWITTER_API_KEY");
            string apiSecret = Environment.GetEnvironmentVariable("TWITTER_API_SECRET");
            string bearerToken = Environment.GetEnvironmentVariable("TWITTER_BEARER_TOKEN");

            _client = new TwitterClient(apiKey, apiSecret, bearerToken);
            Log.Information($"[{nameof(TwitterService)}] Connected to the Twitter API");
        }

        public void Execute()
        {
            ExecuteAsync().GetAwaiter().GetResult();
        }

        private async Task ExecuteAsync()
        {
            Log.Debug($"[{nameof(TwitterService)}] Getting tweets");

            Dictionary<long, List<ITweet>> userTweetPairs = new Dictionary<long, List<ITweet>>();
            using BotContext db = new BotContext();
            DateTimeOffset lastFetch = db.ActualBotSettings.TimeOfLastTwitterFetch;

            // Load all of the twitter users we should relay tweets from
            foreach (GuildTwitterSettings settings in db.GuildTwitterSettings)
            {
                foreach (long userId in settings.TwitterUserIds)
                {
                    if (userTweetPairs.ContainsKey(userId))
                    {
                        await PostTweetsToChannel(settings, userTweetPairs[userId]).ConfigureAwait(false);
                    }
                    else
                    {
                        List<ITweet> userTweets = await GetUserTweets(userId, lastFetch).ConfigureAwait(false);
                        userTweetPairs.Add(userId, userTweets);
                        await PostTweetsToChannel(settings, userTweets).ConfigureAwait(false);
                    }
                }
            }

            // Update our time record
            db.ActualBotSettings.TimeOfLastTwitterFetch = DateTimeOffset.Now;
            await db.SaveChangesAsync().ConfigureAwait(false);

            Log.Information($"[{nameof(TwitterService)}] Finished getting tweets");
        }

        /// <summary>
        /// Gets tweets from a twitter user made after the specified fetch time
        /// </summary>
        /// <param name="userId">The ID of the twitter user to get tweets from</param>
        /// <param name="lastFetch">The earliest time to fetch tweets from</param>
        /// <returns></returns>
        private async Task<List<ITweet>> GetUserTweets(long userId, DateTimeOffset lastFetch)
        {
            ITweet[] tweets = await _client.Timelines.GetUserTimelineAsync(userId).ConfigureAwait(false);

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

        private static async Task PostTweetsToChannel(GuildTwitterSettings settings, List<ITweet> tweets)
        {
            DiscordChannel channel;
            try
            {
                channel = await Program.Client.GetChannelAsync(settings.RelayChannelId).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                channel = (await Program.Client.GetGuildAsync(settings.Guild.Id).ConfigureAwait(false)).GetDefaultChannel();
                // TODO: Append the channel reset command
                await channel.SendMessageAsync($":warning: {Program.NAME} can't find the Twitter relay channel. Has it been deleted? Please reset it.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[{nameof(TwitterService)}] Could not get channel to send tweets to");
                return;
            }

            try
            {
                foreach (ITweet tweet in tweets)
                {
                    // TODO: Experiment with building a full message, rather than relying on the webhook to render
                    await channel.SendMessageAsync(tweet.Url).ConfigureAwait(false);
                }
            } catch (Exception ex)
            {
                Log.Error(ex, $"[{nameof(TwitterService)}] Could not send tweet");
            }
        }
    }
}
