using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.EventStream;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class MetagameEventResponder : ICensusEventHandler<ServiceMessage<MetagameEvent>>
{
    private readonly IMemoryCache _cache;

    public MetagameEventResponder(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task HandleAsync(ServiceMessage<MetagameEvent> censusEvent, CancellationToken ct = default)
    {
        object key = CacheKeyHelpers.GetMetagameEventKey(censusEvent.Payload);
        _cache.Set(key, censusEvent.Payload);

        return Task.CompletedTask;
    }
}
