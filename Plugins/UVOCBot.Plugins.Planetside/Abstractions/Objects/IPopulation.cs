using DbgCensus.Core.Objects;
using System;
using System.Collections.Generic;

namespace UVOCBot.Plugins.Planetside.Abstractions.Objects;

/// <summary>
/// Represents a world population.
/// </summary>
public interface IPopulation
{
    /// <summary>
    /// Gets the world that this population measure is for.
    /// </summary>
    WorldDefinition WorldId { get; }

    /// <summary>
    /// Gets the per-faction population count.
    /// </summary>
    Dictionary<FactionDefinition, int> Population { get; }

    /// <summary>
    /// Gets the total player count.
    /// </summary>
    int Total { get; }

    /// <summary>
    /// Gets the time that the population counts were last updated.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the name of the source of the population counts.
    /// </summary>
    string SourceName { get; }
}
