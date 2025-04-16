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

    public override async Task<Result<IReadOnlyDictionary<string, MapRotations>>> GetMapRotationsAsync(CancellationToken ct = default)
    {
        _cache.TryGetValue
        (
            CacheKeyHelpers.GetMapRotationBundleKey(),
            out IReadOnlyDictionary<string, MapRotations>? bundle
        );
        if (bundle is not null)
            return Result<IReadOnlyDictionary<string, MapRotations>>.FromSuccess(bundle);

        Result<IReadOnlyDictionary<string, MapRotations>> getRotations = await base.GetMapRotationsAsync(ct)
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
}
