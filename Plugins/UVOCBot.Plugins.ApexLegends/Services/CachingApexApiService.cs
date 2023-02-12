using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Objects;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Services;

public sealed class CachingApexApiService : ApexApiService
{
    private readonly IMemoryCache _cache;

    public CachingApexApiService(HttpClient client, IMemoryCache cache)
        : base(client)
    {
        _cache = cache;
    }

    public override async Task<Result<MapRotationBundle>> GetMapRotationsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetMapRotationBundleKey(), out MapRotationBundle? bundle))
            return bundle;

        Result<MapRotationBundle> getRotations = await base.GetMapRotationsAsync(ct)
            .ConfigureAwait(false);

        if (getRotations.IsDefined())
        {
            _cache.Set
            (
                CacheKeyHelpers.GetMapRotationBundleKey(),
                getRotations.Entity,
                CacheEntryHelpers.GetMapRotationBundleOptions(getRotations.Entity)
            );
        }

        return getRotations;
    }

    public override async Task<Result<List<CraftingBundle>>> GetCraftingBundlesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetCraftingBundleKey(), out List<CraftingBundle>? bundles))
            return bundles;

        Result<List<CraftingBundle>> getBundles = await base.GetCraftingBundlesAsync(ct)
            .ConfigureAwait(false);

        if (getBundles.IsDefined())
        {
            _cache.Set
            (
                CacheKeyHelpers.GetCraftingBundleKey(),
                getBundles.Entity,
                CacheEntryHelpers.GetCraftingBundlesOptions(getBundles.Entity)
            );
        }

        return getBundles;
    }
}
