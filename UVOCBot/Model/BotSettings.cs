using System;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains settings pertinent to the bot's services
    /// </summary>
    public sealed class BotSettings
    {
        public static BotSettings Default => new BotSettings
        {
            TimeOfLastTwitterFetch = DateTimeOffset.UtcNow
        };

        public int Id { get; set; }

        public DateTimeOffset TimeOfLastTwitterFetch { get; set; }
    }
}
