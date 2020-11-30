using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains settings pertinent to a guild's preferences
    /// </summary>
    public sealed class GuildSettings
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        public ulong Id { get; set; }
    }
}
