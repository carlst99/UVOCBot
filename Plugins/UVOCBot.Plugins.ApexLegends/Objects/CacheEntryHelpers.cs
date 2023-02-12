using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public static MemoryCacheEntryOptions GetCraftingBundlesOptions(IEnumerable<CraftingBundle> bundles)
        => new()
        {
            AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(bundles.Where(x => x.End > 0).Min(x => x.End)),
            Priority = CacheItemPriority.Normal,
            Size = 1
        };
}
