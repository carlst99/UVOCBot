using System.Collections.Generic;

namespace UVOCBot.Core.Model
{
    public class GuildTwitterSettingsDTO
    {
        /// <summary>
        /// Gets or sets the guild that these Twitter settings are for
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord id of the guild channel in which Twitter posts should be relayed to
        /// </summary>
        public ulong? RelayChannelId { get; set; }

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a list of twitter user ids from whom to relay posts
        /// </summary>
        public IReadOnlyList<long> TwitterUsers { get; set; } = new List<long>().AsReadOnly();

        public GuildTwitterSettingsDTO()
        {
        }

        public GuildTwitterSettingsDTO(ulong guildId)
        {
            GuildId = guildId;
        }

        public override bool Equals(object obj) => obj is GuildTwitterSettingsDTO s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}
