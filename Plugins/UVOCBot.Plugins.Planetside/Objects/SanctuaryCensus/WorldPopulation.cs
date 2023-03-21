using DbgCensus.Core.Objects;
using System;
using System.Collections.Generic;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

public record WorldPopulation
(
    WorldDefinition WorldId,
    DateTimeOffset Timestamp,
    int Total,
    Dictionary<FactionDefinition, int> Population
) : IPopulation;
