using DbgCensus.Core.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.Honu;

/// <inheritdoc cref="IPopulation"/>
public record HonuPopulation
(
    WorldDefinition WorldId,
    int NC,

    [property: JsonPropertyName("nsOther")]
    int NS,

    [property: JsonPropertyName("ns_nc")]
    int NSNC,

    [property: JsonPropertyName("ns_tr")]
    int NSTR,

    [property: JsonPropertyName("ns_vs")]
    int NSVS,
    int TR,
    int VS,

    [property: JsonPropertyName("timestamp")]
    string HonuTimestamp
) : IPopulation
{
    private Dictionary<FactionDefinition, int>? _population;

    /// <inheritdoc />
    public Dictionary<FactionDefinition, int> Population
        => _population ??= new Dictionary<FactionDefinition, int>
        {
            { FactionDefinition.VS, VS + NSVS },
            { FactionDefinition.NC, NC + NSNC },
            { FactionDefinition.TR, TR + NSTR },
            { FactionDefinition.NSO, NS }
        };

    /// <inheritdoc />
    public int Total => Population.Values.Sum();

    /// <inheritdoc />
    [JsonIgnore]
    public DateTimeOffset Timestamp => DateTimeOffset.Parse(HonuTimestamp, null, DateTimeStyles.AssumeUniversal);

    /// <inheritdoc />
    public string SourceName => "Honu";
}
