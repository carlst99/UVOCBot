using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery
{
    public record World
    {
        public WorldDefinition WorldId { get; init; }
        public string State { get; init; }
        public GlobalisedString Name { get; init; }

        public World()
        {
            State = string.Empty;
            Name = new GlobalisedString();
        }
    }
}
