using System;
using System.Collections.Generic;

namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

public record OutfitWarRoundWithMatches
(
    uint OutfitWarID,
    ulong PrimaryRoundID,
    ulong RoundID,
    uint Order,
    string Stage,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyList<OutfitWarMatch> Matches
);
