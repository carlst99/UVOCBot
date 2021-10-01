using DbgCensus.Core.Objects;
using System.Diagnostics.CodeAnalysis;
using UVOCBot.Plugins.Planetside.Objects.EventStream;

namespace UVOCBot.Plugins.Planetside.Services
{
    public class MetagameEventInjectionService
    {
        private readonly Dictionary<WorldDefinition, Dictionary<ZoneDefinition, MetagameEvent>> _store;

        public MetagameEventInjectionService()
        {
            _store = new Dictionary<WorldDefinition, Dictionary<ZoneDefinition, MetagameEvent>>();

            foreach (WorldDefinition wDef in Enum.GetValues<WorldDefinition>())
                _store.Add(wDef, new Dictionary<ZoneDefinition, MetagameEvent>());
        }

        public void Set(MetagameEvent metagameEvent)
        {
            _store[metagameEvent.WorldID][metagameEvent.ZoneID.Definition] = metagameEvent;
        }

        public bool TryGet(WorldDefinition world, ZoneDefinition zone, [NotNullWhen(true)] out MetagameEvent? metagameEvent)
            => _store[world].TryGetValue(zone, out  metagameEvent);
    }
}
