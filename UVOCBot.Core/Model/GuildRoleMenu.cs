using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model
{
    public class GuildRoleMenu : IGuildObject
    {
        /// <summary>
        /// Gets or sets the ID of the guild that this role menu belongs to.
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the channel in which this role menu is.
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the role menu message.
        /// </summary>
        public ulong MessageId { get; set; }
    }
}
