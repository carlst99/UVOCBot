using System;

namespace UVOCBot.Model.Census
{
    /// <summary>
    /// The query model for https://census.daybreakgames.com/get/ps2/world_event?type=METAGAME&world_id=1&c:limit=20&c:sort=timestamp&c:join=world%5Einject_at:world
    /// </summary>
    public record MetagameEvent
    {
        public int MetagameEventId { get; init; }
        public int MetagameEventState { get; init; }
        public double FactionNC { get; init; }
        public double FactionTR { get; init; }
        public double FactionVS { get; init; }
        public int ExperienceBonus { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public ZoneType ZoneId { get; init; }
        public WorldType WorldId { get; init; }
        public int InstanceId { get; init; }
        public string MetagameEventStateName { get; init; }
        public World World { get; init; }

        public MetagameEvent()
        {
            MetagameEventStateName = string.Empty;
            World = new World();
        }
    }
}
