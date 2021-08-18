namespace UVOCBot.Core.Dto
{
    public class PlanetsideSettingsDto
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the default world to use
        /// </summary>
        public int? DefaultWorld { get; set; }

        public PlanetsideSettingsDto()
        {
        }

        public PlanetsideSettingsDto(ulong guildId)
        {
            GuildId = guildId;
            DefaultWorld = null;
        }

        public override bool Equals(object? obj)
            => obj is PlanetsideSettingsDto s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}
