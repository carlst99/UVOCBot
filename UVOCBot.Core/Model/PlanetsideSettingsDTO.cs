namespace UVOCBot.Core.Model
{
    public class PlanetsideSettingsDTO
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the default world to use
        /// </summary>
        public int? DefaultWorld { get; set; }

        public PlanetsideSettingsDTO()
        {
        }

        public PlanetsideSettingsDTO(ulong guildId)
        {
            GuildId = guildId;
            DefaultWorld = null;
        }

        public override bool Equals(object obj) => obj is PlanetsideSettingsDTO s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}
