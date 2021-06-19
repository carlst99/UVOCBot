using System.ComponentModel.DataAnnotations;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Model
{
    public class PlanetsideSettings
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the default world to use
        /// </summary>
        public int? DefaultWorld { get; set; }

        public PlanetsideSettingsDTO ToDto()
            => new()
            {
                GuildId = GuildId,
                DefaultWorld = DefaultWorld
            };

        public static PlanetsideSettings FromDto(PlanetsideSettingsDTO dto)
            => new()
            {
                GuildId = dto.GuildId,
                DefaultWorld = dto.DefaultWorld
            };
    }
}
