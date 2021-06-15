using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Model
{
    [Index(nameof(GroupName), IsUnique = false)]
    public class MemberGroup
    {
        public const int MAX_LIFETIME_HOURS = 24;

        [Key]
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets ID of the guild that this group belongs to
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// The ID of the Discord user that created this group
        /// </summary>
        public ulong CreatorId { get; set; }

        /// <summary>
        /// Gets or sets the time that this group was created at
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the name of the group
        /// </summary>
        public string GroupName { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// An LF delimited list of the users in this group
        /// </summary>
        public string UserIds { get; set; } = string.Empty;

        public MemberGroupDTO ToDto()
            => new()
            {
                Id = Id,
                GuildId = GuildId,
                CreatorId = CreatorId,
                CreatedAt = CreatedAt,
                GroupName = GroupName,
                UserIds = new List<ulong>(UserIds.Split('\n').Select(s => ulong.Parse(s)))
            };

        public static MemberGroup FromDto(MemberGroupDTO dto)
            => new()
            {
                Id = dto.Id,
                GuildId = dto.GuildId,
                CreatorId = dto.CreatorId,
                CreatedAt = dto.CreatedAt,
                GroupName = dto.GroupName,
                UserIds = string.Join('\n', dto.UserIds.Select(i => i.ToString()))
            };
    }
}
