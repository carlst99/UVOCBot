using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains guild-specific settings regarding their twitter preferences
    /// </summary>
    public sealed class GuildTwitterSettings
    {
        /// <summary>
        /// Gets or sets the guild that these Twitter settings are for
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord id of the guild channel in which Twitter posts should be relayed to
        /// </summary>
        public ulong? RelayChannelId { get; set; }

        /// <summary>
        /// Gets or sets a list of twitter user ids from whom to relay posts
        /// </summary>
        public ICollection<TwitterUser> TwitterUsers { get; } = new List<TwitterUser>();

        public GuildTwitterSettings() { }

        public GuildTwitterSettings(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}
