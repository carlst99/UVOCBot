using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

public class PlanetsideSettings : IGuildObject
{
    /// <inheritdoc />
    [Key]
    public ulong GuildId { get; init; }

    /// <summary>
    /// Gets or sets the default world to use
    /// </summary>
    public int? DefaultWorld { get; set; }

    /// <summary>
    /// Gets or sets a list of outfits that should be tracked.
    /// </summary>
    public List<ulong> TrackedOutfits { get; set; }

    /// <summary>
    /// Gets or sets the channel to post base capture notifications to.
    /// </summary>
    public ulong? BaseCaptureChannelId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanetsideSettings"/> class.
    /// </summary>
    public PlanetsideSettings()
    {
        TrackedOutfits = new List<ulong>();
    }
}
