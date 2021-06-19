namespace UVOCBot.Core.Model
{
    public sealed class GuildSettingsDTO
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// The prefix used to access bot commands
        /// </summary>
        public string? Prefix { get; set; }

        public GuildSettingsDTO()
        {
        }

        public GuildSettingsDTO(ulong guildId)
        {
            GuildId = guildId;
            Prefix = null;
        }

        public override bool Equals(object? obj)
            => obj is GuildSettingsDTO s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}
