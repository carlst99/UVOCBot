using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

[Index(nameof(GroupName), IsUnique = false)]
public class MemberGroup : IGuildObject
{
    /// <summary>
    /// Gets the maximum length of time that a group can exist for.
    /// </summary>
    public const int MAX_LIFETIME_HOURS = 24;

    [Key]
    public ulong Id { get; set; }

    /// <inheritdoc />
    public ulong GuildId { get; set; }

    /// <summary>
    /// The ID of the Discord user that created this group.
    /// </summary>
    public ulong CreatorId { get; set; }

    /// <summary>
    /// Gets or sets the time that this group was created at.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    public string GroupName { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the list of users in this group.
    /// </summary>
    public List<ulong> UserIds { get; set; }

    /// <summary>
    /// Only use this constructor if you are setting the <see cref="GuildId"/> immediately after construction.
    /// </summary>
    public MemberGroup()
        : this(0, "Group")
    {
    }

    public MemberGroup(ulong guildId, string groupName)
    {
        GuildId = guildId;
        UserIds = new List<ulong>();
        CreatedAt = DateTimeOffset.UtcNow;
        GroupName = groupName;
    }
}
