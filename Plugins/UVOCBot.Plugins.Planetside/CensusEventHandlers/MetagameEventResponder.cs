using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using Microsoft.Extensions.Caching.Memory;
using UVOCBot.Plugins.Planetside.Objects.EventStream;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers
{
    internal sealed class MetagameEventResponder : ICensusEventHandler<ServiceMessage<MetagameEvent>>
    {
        private readonly IMemoryCache _cache;

        public MetagameEventResponder(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task HandleAsync(ServiceMessage<MetagameEvent> censusEvent, CancellationToken ct = default)
        {
            _cache.Set(censusEvent.Payload.GetCacheKey(), censusEvent.Payload);

            return Task.CompletedTask;
        }
    }
}
