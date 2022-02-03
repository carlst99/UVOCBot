using DbgCensus.Core.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;

namespace UVOCBot.Plugins.Planetside.Objects;

/// <inheritdoc cref="IPopulation"/>
public record HonuPopulation
(
    WorldDefinition WorldID,
    int NC,
    int? NS,
    int TR,
    int VS,
    int Total
) : IPopulation;
