using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

public record MinimalCharacter
(
    ulong CharacterID,
    Name Name,
    FactionDefinition FactionID
);
