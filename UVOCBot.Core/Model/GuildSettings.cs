using System.ComponentModel.DataAnnotations;
using UVOCBot.Core.Dto;

namespace UVOCBot.Core.Model
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
        /// Gets or sets the prefix used to access bot commands
        /// </summary>
        public string? Prefix { get; set; }

        public GuildSettingsDto ToDto()
            => new()
            {
                GuildId = GuildId,
                Prefix = Prefix
            };

        public static GuildSettings FromDto(GuildSettingsDto dto)
            => new()
            {
                GuildId = dto.GuildId,
                Prefix = dto.Prefix
            };
    }
}
