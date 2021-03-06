﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Model
{
    /// <summary>
    /// Contains guild-specific settings regarding their twitter preferences
    /// </summary>
    public sealed class GuildTwitterSettings
    {
        /// <summary>
        /// Gets or sets the guild that these Twitter settings are for
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord id of the guild channel in which Twitter posts should be relayed to
        /// </summary>
        public ulong? RelayChannelId { get; set; }

        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a list of twitter user ids from whom to relay posts
        /// </summary>
        public IList<TwitterUser> TwitterUsers { get; set; } = new List<TwitterUser>();

        public GuildTwitterSettings()
        {
            IsEnabled = true;
            RelayChannelId = null;
        }

        public GuildTwitterSettings(ulong guildId)
        {
            GuildId = guildId;
            RelayChannelId = null;
            IsEnabled = true;
        }

        public GuildTwitterSettingsDTO ToDto()
            => new()
            {
                GuildId = GuildId,
                IsEnabled = IsEnabled,
                RelayChannelId = RelayChannelId,
                TwitterUsers = TwitterUsers.Select(u => u.UserId).ToList()
            };

        public static GuildTwitterSettings FromDto(GuildTwitterSettingsDTO dto)
            => new()
            {
                GuildId = dto.GuildId,
                IsEnabled = dto.IsEnabled,
                RelayChannelId = dto.RelayChannelId
            };

        public override bool Equals(object? obj) => obj is GuildTwitterSettings s
            && s.GuildId.Equals(GuildId);

        public override int GetHashCode() => GuildId.GetHashCode();
    }
}
