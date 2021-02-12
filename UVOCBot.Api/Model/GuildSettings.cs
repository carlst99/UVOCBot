using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Api.Model
{
    /// <summary>
    /// Contains settings pertinent to a guild's preferences
    /// </summary>
    public sealed class GuildSettings
    {
        /// <summary>
        /// Gets or sets the Discord ID of this guild
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the channel to send users to when the bonk command is used
        /// </summary>
        public ulong? BonkChannelId { get; set; }

        /// <summary>
        /// Gets or sets the prefix used to access bot commands
        /// </summary>
        public string Prefix { get; set; }
    }
}
