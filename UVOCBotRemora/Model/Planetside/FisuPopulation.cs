using System.Collections.Generic;

namespace UVOCBotRemora.Model.Planetside
{
    public class FisuPopulation
    {
        public class ApiResult
        {
            public WorldType WorldId { get; set; }
            public int VS { get; set; }
            public int NC { get; set; }
            public int TR { get; set; }
            public int NS { get; set; }
        }

        public List<ApiResult> Result { get; set; }

        public WorldType World => Result[0].WorldId;
        public int VS => Result[0].VS;
        public int NC => Result[0].NC;
        public int TR => Result[0].TR;
        public int NS => Result[0].NS;
        public int Total => VS + NC + TR + NS;
    }
}
