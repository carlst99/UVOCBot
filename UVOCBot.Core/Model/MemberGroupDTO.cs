using System;
using System.Collections.Generic;

namespace UVOCBot.Core.Model
{
    public class MemberGroupDTO
    {
        public const int MAX_LIFETIME_HOURS = 24;

        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets ID of the guild that this group belongs to
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the UTC time that this group was created at
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the name of the group
        /// </summary>
        public string GroupName { get; set; }

        public List<ulong> UserIds { get; set; }

        public MemberGroupDTO() { }

        public MemberGroupDTO(string groupName, ulong guildId, List<ulong> userIds)
        {
            GroupName = groupName;
            GuildId = guildId;
            UserIds = userIds;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
