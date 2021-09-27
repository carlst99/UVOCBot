using DbgCensus.Rest.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Results;
using UVOCBot.Plugins.Planetside.Objects.Census.Map;
using UVOCBot.Plugins.Planetside.Objects.Census.Outfit;

namespace UVOCBot.Plugins.Planetside.Services
{
    /// <summary>
    /// <inheritdoc cref="CensusApiService" />
    /// Some queries performed through this service may be cached.
    /// </summary>
    public class CachingCensusApiService : CensusApiService
    {
        private readonly IMemoryCache _cache;

        public CachingCensusApiService(
            ILogger<CensusApiService> logger,
            IQueryService queryService,
            IMemoryCache cache)
            : base(logger, queryService)
        {
            _cache = cache;
        }

        /// <summary>
        /// <inheritdoc />
        /// This query is cached.
        /// </summary>
        public async override Task<Result<Outfit?>> GetOutfitAsync(ulong id, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(GetOutfitCacheKey(id), out Outfit outfit))
                return Result<Outfit?>.FromSuccess(outfit);

            Result<Outfit?> getOutfit = await base.GetOutfitAsync(id, ct).ConfigureAwait(false);

            if (getOutfit.IsDefined())
            {
                _cache.Set(
                    GetOutfitCacheKey(id),
                    getOutfit.Entity,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
                        Priority = CacheItemPriority.Low
                    });
            }

            return getOutfit;
        }

        /// <summary>
        /// <inheritdoc />
        /// This query is cached.
        /// </summary>
        public override async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(GetFacilityCacheKey(facilityID), out MapRegion region))
                return region;

            Result<MapRegion?> getMapRegionResult = await base.GetFacilityRegionAsync(facilityID, ct).ConfigureAwait(false);

            if (getMapRegionResult.IsDefined())
            {
                _cache.Set(
                    GetFacilityCacheKey(facilityID),
                    getMapRegionResult.Entity,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
                        Priority = CacheItemPriority.Low
                    });
            }

            return getMapRegionResult;
        }
        private static object GetOutfitCacheKey(ulong outfitId)
            => (typeof(Outfit), outfitId);

        private static object GetFacilityCacheKey(ulong facilityID)
            => (typeof(MapRegion), facilityID);
    }
}
