using System;
using System.Text.Json.Serialization;

namespace UVOCBot.Model
{
    public class BotSettings : ISettings
    {
        [JsonIgnore]
        public ISettings Default => new BotSettings
        {
            TimeOfLastTweetFetch = DateTimeOffset.Now
        };

        public DateTimeOffset TimeOfLastTweetFetch { get; set; }
    }
}
