using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Api.Model
{
    /// <summary>
    /// Contains settings pertinent to a guild's preferences
    /// </summary>
    public sealed class GuildSettings
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }
    }
}
