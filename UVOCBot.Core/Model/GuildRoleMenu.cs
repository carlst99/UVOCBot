using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

[Index(nameof(GuildId), nameof(MessageId))]
public class GuildRoleMenu : IGuildObject
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the guild that this role menu belongs to.
    /// </summary>
    public ulong GuildId { get; init; }

    /// <summary>
    /// Gets or sets the ID of the person who created the role menu.
    /// </summary>
    public ulong AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the channel in which this role menu is.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the role menu message.
    /// </summary>
    public ulong MessageId { get; set; }

    /// <summary>
    /// Gets or sets the title of the role menu.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the role menu.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The roles contained in this menu.
    /// </summary>
    public List<GuildRoleMenuRole> Roles { get; set; }

    public GuildRoleMenu(ulong guildId, ulong messageId, ulong channelId, ulong authorId, string title)
    {
        GuildId = guildId;
        MessageId = messageId;
        ChannelId = channelId;
        AuthorId = authorId;
        Title = title;
        Description = string.Empty;
        Roles = new List<GuildRoleMenuRole>();
    }
}
