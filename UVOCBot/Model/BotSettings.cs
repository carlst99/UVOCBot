using Realms;
using System;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains settings pertinent to the bot's services
    /// </summary>
    public sealed class BotSettings : RealmObject
    {
        public static BotSettings Default => new BotSettings
        {
            TimeOfLastTwitterFetch = DateTimeOffset.Now
        };

        public DateTimeOffset TimeOfLastTwitterFetch { get; set; }
    }
}
