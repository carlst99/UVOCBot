using DbgCensus.Core.Objects;
using System.Collections.Generic;
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
        int NC,
        int NS,
        int TR,
        int VS
    );

    /// <inheritdoc />
    public WorldDefinition World => Result[0].WorldID;

    /// <inheritdoc />
    public int NC => Result[0].NC;

    /// <inheritdoc />
    public int? NS => Result[0].NS;

    /// <inheritdoc />
    public int VS => Result[0].VS;

    /// <inheritdoc />
    public int TR => Result[0].TR;

    /// <inheritdoc />
    public int Total => VS + NC + TR + (int)NS!;
}
