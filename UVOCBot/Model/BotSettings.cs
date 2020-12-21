using System;

namespace UVOCBot.Model
{
    public class BotSettings : ISettings
    {
        public ISettings Default { get; }
        public DateTimeOffset TimeOfLastTweetFetch { get; set; }
    }
}
