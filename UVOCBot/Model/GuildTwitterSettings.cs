using Realms;
using System.Collections.Generic;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains guild-specific settings regarding their twitter preferences
    /// </summary>
    public sealed class GuildTwitterSettings : RealmObject
    {
        /// <summary>
        /// Gets or sets the guild that these Twitter settings are for
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the guild channel in which Twitter posts should be relayed to
        /// </summary>
        public ulong RelayChannelId { get; set; }

        /// <summary>
        /// Gets or sets a list of users from whom to relay Twitter posts
        /// </summary>
        public IList<long> TwitterUserIds { get; }
    }
}
