using System.ComponentModel.DataAnnotations;

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
    }
}
