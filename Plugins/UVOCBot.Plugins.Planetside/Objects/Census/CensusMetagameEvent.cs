using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.Census
{
    // TODO: Modify for pure metagame event requests. Might be able to botch it and perform request in StartupWorker using the event stream class.
    /// <summary>
    /// The query model for https://census.daybreakgames.com/get/ps2/world_event?type=METAGAME&world_id=1&c:limit=20&c:sort=timestamp&c:join=world%5Einject_at:world
    /// </summary>
    public record CensusMetagameEvent
    {
        public int MetagameEventId { get; init; }
        public int MetagameEventState { get; init; }
        public double FactionNC { get; init; }
        public double FactionTR { get; init; }
        public double FactionVS { get; init; }
        public double ExperienceBonus { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public ZoneId ZoneId { get; init; }
        public WorldDefinition WorldId { get; init; }
        public int InstanceId { get; init; }
        public string MetagameEventStateName { get; init; }
        public World World { get; init; }

        public CensusMetagameEvent()
        {
            MetagameEventStateName = string.Empty;
            ZoneId = ZoneId.Default;
            World = new World();
        }
    }
}
