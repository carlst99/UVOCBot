using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

public class SpaceEngineersData : IGuildObject
{
    /// <inheritdoc />
    [Key]
    public ulong GuildId { get; init; }

    /// <summary>
    /// The address of the VRage remote API endpoint.
    /// </summary>
    public string? ServerAddress { get; set; }

    /// <summary>
    /// The port of the VRage remote API endpoint.
    /// </summary>
    public int? ServerPort { get; set; }

    /// <summary>
    /// The authentication key of the VRage remote API endpoint.
    /// </summary>
    public string? ServerKey { get; set; }

    /// <summary>
    /// The ID of the channel in which the status message is posted.
    /// </summary>
    public ulong? StatusMessageChannelId { get; set; }

    /// <summary>
    /// The ID of the status message.
    /// </summary>
    public ulong? StatusMessageId { get; set; }
}
