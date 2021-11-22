using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class MetagameEventResponder : IPayloadHandler<IMetagameEvent>
{
    private readonly IMemoryCache _cache;

    public MetagameEventResponder(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task HandleAsync(IMetagameEvent censusEvent, CancellationToken ct = default)
    {
        object key = CacheKeyHelpers.GetMetagameEventKey(censusEvent);
        _cache.Set(key, censusEvent);

        return Task.CompletedTask;
    }
}
