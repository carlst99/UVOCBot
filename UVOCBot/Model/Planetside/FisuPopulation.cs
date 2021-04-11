using System.Collections.Generic;

namespace UVOCBot.Model.Planetside
{
    public record FisuPopulation
    {
        public record ApiResult
        {
            public WorldType WorldId { get; init; }
            public int VS { get; init; }
            public int NC { get; init; }
            public int TR { get; init; }
            public int NS { get; init; }
        }

        public List<ApiResult> Result { get; init; }

        public FisuPopulation()
        {
            Result = new List<ApiResult>()
            {
                new ApiResult()
            };
        }

        public WorldType World => Result[0].WorldId;
        public int VS => Result[0].VS;
        public int NC => Result[0].NC;
        public int TR => Result[0].TR;
        public int NS => Result[0].NS;
        public int Total => VS + NC + TR + NS;
    }
}
