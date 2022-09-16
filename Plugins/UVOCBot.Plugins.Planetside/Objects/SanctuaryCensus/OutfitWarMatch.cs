using DbgCensus.Core.Objects;
using System;

namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

public record OutfitWarMatch
(
    uint OutfitWarID,
    ulong RoundID,
    ulong MatchID,
    ulong OutfitAId,
    ulong OutfitBId,
    DateTimeOffset StartTime,
    uint Order,
    WorldDefinition WorldID,
    FactionDefinition OutfitAFactionID,
    FactionDefinition OutfitBFactionID
);
