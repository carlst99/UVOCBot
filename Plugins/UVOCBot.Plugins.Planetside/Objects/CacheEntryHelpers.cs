using Microsoft.Extensions.Caching.Memory;

namespace UVOCBot.Plugins.Planetside.Objects
{
    public static class CacheEntryHelpers
    {
        public static MemoryCacheEntryOptions GetMetagameEventOptions()
            => new()
            {
                SlidingExpiration = TimeSpan.FromDays(1),
                Priority = CacheItemPriority.Low,
                Size = 1
            };

        public static MemoryCacheEntryOptions GetOutfitOptions()
            => new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
                Priority = CacheItemPriority.Low,
                Size = 1
            };

        public static MemoryCacheEntryOptions GetMapRegionOptions()
            => new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
                Priority = CacheItemPriority.Low,
                Size = 1
            };

        public static MemoryCacheEntryOptions GetMapOptions()
            => new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
                Priority = CacheItemPriority.Normal,
                Size = 2
            };

        public static MemoryCacheEntryOptions GetFisuPopulationOptions()
            => new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
                Priority = CacheItemPriority.Low,
                Size = 1
            };
    }
}
