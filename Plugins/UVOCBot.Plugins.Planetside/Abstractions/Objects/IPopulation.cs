using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Abstractions.Objects;

/// <summary>
/// Represents a world population.
/// </summary>
public interface IPopulation
{
    /// <summary>
    /// Gets the world that this population measure is for.
    /// </summary>
    WorldDefinition World { get; }

    /// <summary>
    /// Gets the NC player count.
    /// </summary>
    int NC { get; }

    /// <summary>
    /// Gets the NS player count. Can be null, depending on whether the data source
    /// identifies which faction the NS characters are playing with.
    /// </summary>
    int? NS { get; }

    /// <summary>
    /// Gets the TR player count.
    /// </summary>
    int TR { get; }

    /// <summary>
    /// Gets the VS player count.
    /// </summary>
    int VS { get; }

    /// <summary>
    /// Gets the total player count.
    /// </summary>
    int Total { get; }
}
