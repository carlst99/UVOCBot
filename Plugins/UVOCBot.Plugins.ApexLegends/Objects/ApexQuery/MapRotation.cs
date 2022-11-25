using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

public record MapRotation
(
    [property: JsonPropertyName("start")] long Start,
    [property: JsonPropertyName("end")] long End,
    [property: JsonPropertyName("map")] string Map,
    int DurationInSecs,
    [property: JsonPropertyName("asset")] string? Asset,
    [property: JsonPropertyName("remianingSecs")] int? RemainingSecs
);

public record MapRotationBundle
(
    MapRotation Current,
    MapRotation? Next
);
