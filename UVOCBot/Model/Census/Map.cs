using System.Collections.Generic;

namespace UVOCBot.Model.Census
{
    /// <summary>
    /// The query model for https://census.daybreakgames.com/s:WVWCeOP4oeLb/get/ps2/map?world_id=1&zone_ids=2,4,6,8
    /// </summary>
    public record Map
    {
        public record RegionModel
        {
            public record RowDataModel
            {
                public int RegionId { get; init; }
                public Faction FactionId { get; init; }
            }

            public bool IsList { get; init; }
            public IEnumerable<RowDataModel> Row { get; init; }

            public RegionModel()
            {
                Row = new List<RowDataModel>();
            }
        }

        public ZoneType ZoneId { get; init; }
        public RegionModel Regions { get; init; }
    }
}
