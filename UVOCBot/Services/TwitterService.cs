using System;
using System.Collections.Generic;
using System.Text;
using Tweetinvi;

namespace UVOCBot.Services
{
    public class TwitterService
    {
        protected readonly TwitterClient _client;

        public TwitterService()
        {
            string apiKey = Environment.GetEnvironmentVariable("TWITTER_API_KEY");
            string apiSecret = Environment.GetEnvironmentVariable("TWITTER_API_SECRET");
            _client = new TwitterClient()
        }
    }
}
