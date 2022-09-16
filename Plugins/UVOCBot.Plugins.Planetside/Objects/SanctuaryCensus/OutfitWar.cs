using DbgCensus.Core.Objects;
using System;

namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

public record OutfitWar
(
    uint OutfitWarID,
    WorldDefinition WorldID,
    GlobalizedString Title,
    uint OutfitSignupRequirement,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string ImagePath
);
