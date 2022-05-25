using DbgCensus.Core.Objects;
using System.Text.Json.Serialization;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;

namespace UVOCBot.Plugins.Planetside.Objects;

/// <inheritdoc cref="IPopulation"/>
public record HonuPopulation
(
    WorldDefinition WorldID,
    int NC,

    [property: JsonPropertyName("nsOther")]
    int? NS,

    [property: JsonPropertyName("ns_nc")]
    int NSNC,

    [property: JsonPropertyName("ns_tr")]
    int NSTR,

    [property: JsonPropertyName("ns_vs")]
    int NSVS,
    int TR,
    int VS
) : IPopulation
{
    private readonly int _nc = NC;
    private readonly int _tr = TR;
    private readonly int _vs = VS;

    public int NC => _nc + NSNC;

    public int TR => _tr + NSTR;

    public int VS => _vs + NSVS;

    public int Total => NC + TR + VS + NS ?? 0;
}
