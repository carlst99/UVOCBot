using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains guild-specific settings regarding their twitter preferences
    /// </summary>
    public sealed class GuildTwitterSettings
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the guild that these Twitter settings are for
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord id of the guild channel in which Twitter posts should be relayed to
        /// </summary>
        public ulong? RelayChannelId { get; set; }

        /// <summary>
        /// Gets or sets a list of twitter user ids from whom to relay posts
        /// </summary>
        public IList<TwitterUser> TwitterUsers { get; set; } = new List<TwitterUser>();
    }

    public class TwitterUser
    {
        [Key]
        public int Id { get; set; }

        //[Key]
        public long UserId { get; set; }

        public IList<GuildTwitterSettings> Guilds { get; set; } = new List<GuildTwitterSettings>();

        public TwitterUser() { }

        public TwitterUser(long id) => UserId = id;

        public TwitterUser(string id) => UserId = long.Parse(id);
    }
}
