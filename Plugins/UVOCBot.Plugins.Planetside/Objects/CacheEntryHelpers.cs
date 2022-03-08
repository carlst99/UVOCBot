using Microsoft.Extensions.Caching.Memory;
using System;

namespace UVOCBot.Plugins.Planetside.Objects;

public static class CacheEntryHelpers
{
    public static readonly MemoryCacheEntryOptions MetagameEventOptions = new()
    {
        SlidingExpiration = TimeSpan.FromDays(1),
        Priority = CacheItemPriority.Low,
        Size = 1
    };

    public static readonly MemoryCacheEntryOptions OutfitOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
        Priority = CacheItemPriority.Low,
        Size = 1
    };

    public static readonly MemoryCacheEntryOptions MapRegionOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
        Priority = CacheItemPriority.Low,
        Size = 1
    };

    public static readonly MemoryCacheEntryOptions MapOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        Priority = CacheItemPriority.Normal,
        Size = 2
    };

    public static readonly MemoryCacheEntryOptions PopulationOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
        Priority = CacheItemPriority.Low,
        Size = 1
    };
}
