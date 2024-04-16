using Microsoft.Extensions.Caching.Memory;
using System;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Objects;

public static class CacheEntryHelpers
{
    public static MemoryCacheEntryOptions GetMapRotationBundleOptions(MapRotationBundle bundle)
        => new()
        {
            AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(bundle.Current.End),
            Priority = CacheItemPriority.Normal,
            Size = 1
        };
}
