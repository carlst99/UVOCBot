using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.EventStream
{
    public record FacilityControl
    (
        string EventName,
        DateTimeOffset Timestamp,
        WorldDefinition WorldId,
        Faction OldFactionId,
        Faction NewFactionId,
        ulong OutfitId,
        uint FacilityId,
        ulong DurationHeld,
        ZoneId ZoneId
    );
}
