using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Objects;

public static class CacheEntryHelpers
{
    public static MemoryCacheEntryOptions GetMapRotationBundleOptions(IReadOnlyDictionary<string, MapRotations> bundle)
        => new()
        {
            AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds
                (
                    bundle.Select
                        (
                            // Take the minimum end date as our absolute expiration, else five minutes into the future
                            x => x.Value.Current?.End ?? DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
                        )
                        .Min()
                ),
            Priority = CacheItemPriority.Normal,
            Size = 1
        };
}
