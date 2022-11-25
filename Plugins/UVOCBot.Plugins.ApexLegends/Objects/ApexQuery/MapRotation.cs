using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

public record MapRotation
(
    long Start,
    long End,
    string Map,
    [property: JsonPropertyName("DurationInSecs")] int DurationInSecs,
    string? Asset,
    int? RemainingSecs
);

public record MapRotationBundle
(
    MapRotation Current,
    MapRotation? Next
);
