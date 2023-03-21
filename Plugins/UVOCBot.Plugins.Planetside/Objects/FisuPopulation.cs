using DbgCensus.Core.Objects;
using System.Collections.Generic;
using System.Linq;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using static UVOCBot.Plugins.Planetside.Objects.FisuPopulation;

namespace UVOCBot.Plugins.Planetside.Objects;

/// <inheritdoc cref="IPopulation"/>
public record FisuPopulation
(
    IReadOnlyList<ApiResult> Result
) : IPopulation
{
    public record ApiResult
    (
        WorldDefinition WorldID,
        short NC,
        short NS,
        short TR,
        short VS
    );

    private Dictionary<FactionDefinition, int>? _population;

    /// <inheritdoc />
    public Dictionary<FactionDefinition, int> Population
        => _population ??= new Dictionary<FactionDefinition, int>
            {
                { FactionDefinition.VS, Result[0].VS },
                { FactionDefinition.NC, Result[0].NC },
                { FactionDefinition.TR, Result[0].TR },
                { FactionDefinition.NSO, Result[0].NS },
            };

    /// <inheritdoc />
    public WorldDefinition WorldId => Result[0].WorldID;

    /// <inheritdoc />
    public int Total => Population.Values.Sum();
}
