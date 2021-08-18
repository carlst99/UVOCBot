namespace UVOCBot.Core.Dto
{
    public sealed class GuildSettingsDto
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// The prefix used to access bot commands
        /// </summary>
        public string? Prefix { get; set; }

        public GuildSettingsDto()
        {
        }

        public GuildSettingsDto(ulong guildId)
        {
            GuildId = guildId;
            Prefix = null;
        }

        public override bool Equals(object? obj)
            => obj is GuildSettingsDto s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}
