using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.EventStream
{
    public record PlayerFacilityCapture
    (
        ulong CharacterID,
        string EventName,
        uint FacilityID,
        ulong OutfitID,
        DateTimeOffset Timestamp,
        WorldDefinition WorldID,
        ZoneDefinition ZoneID
    );
}
