using System.Collections.Generic;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains guild-specific settings regarding their twitter preferences
    /// </summary>
    public sealed class GuildTwitterSettings
    {
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the guild that these Twitter settings are for
        /// </summary>
        public GuildSettings Guild { get; set; }

        /// <summary>
        /// Gets or sets the Discord id of the guild channel in which Twitter posts should be relayed to
        /// </summary>
        public ulong RelayChannelId { get; set; }

        /// <summary>
        /// Gets or sets a list of twitter user ids from whom to relay posts
        /// </summary>
        public List<long> TwitterUserIds { get; }
    }
}
