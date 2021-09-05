using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model
{
    public class PlanetsideSettings : IGuildObject
    {
        /// <inheritdoc />
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the default world to use
        /// </summary>
        public int? DefaultWorld { get; set; }

        /// <summary>
        /// Only use this constructor if you are setting the <see cref="GuildId"/> immediately after construction.
        /// </summary>
        public PlanetsideSettings()
            : this(0)
        {
        }

        public PlanetsideSettings(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}
