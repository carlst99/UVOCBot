using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

public class GuildFeedsSettings : IGuildObject
{
    /// <inheritdoc />
    [Key]
    public ulong GuildId { get; init; }

    /// <summary>
    /// Gets or sets a value indicating if feeds are enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the ID of the channel to which feeds will be posted.
    /// </summary>
    public ulong? FeedChannelID { get; set; }

    /// <summary>
    /// Gets or sets the feeds to relay. This is intended to be a bitwise enum field.
    /// </summary>
    public ulong Feeds { get; set; }

    /// <summary>
    /// Do not use this constructor unless you going to set <see cref="GuildId"/> immediately after construction.
    /// </summary>
    public GuildFeedsSettings()
    {
    }

    public GuildFeedsSettings(ulong guildId)
    {
        GuildId = guildId;
    }
}
