﻿namespace UVOCBot.Core.Model
{
    public sealed class GuildSettingsDTO
    {
        /// <summary>
        /// Gets the Discord ID of this guild
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// The channel to send users to when the bonk command is used
        /// </summary>
        public ulong? BonkChannelId { get; set; }

        public GuildSettingsDTO()
        {
        }

        public GuildSettingsDTO(ulong guildId)
        {
            GuildId = guildId;
        }

        public override bool Equals(object obj) => obj is GuildSettingsDTO s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}