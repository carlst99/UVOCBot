using DbgCensus.Core.Objects;
using System.Collections.Generic;

namespace UVOCBot.Plugins.Planetside.Objects.Fisu;

public record Population
{
    public record ApiResult
    {
        public WorldDefinition WorldId { get; init; }
        public int VS { get; init; }
        public int NC { get; init; }
        public int TR { get; init; }
        public int NS { get; init; }
    }

    public IReadOnlyList<ApiResult> Result { get; init; }

    public Population()
    {
        Result = new List<ApiResult>()
            {
                new ApiResult()
            }.AsReadOnly();
    }

    public WorldDefinition World => Result[0].WorldId;
    public int VS => Result[0].VS;
    public int NC => Result[0].NC;
    public int TR => Result[0].TR;
    public int NS => Result[0].NS;
    public int Total => VS + NC + TR + NS;
}
