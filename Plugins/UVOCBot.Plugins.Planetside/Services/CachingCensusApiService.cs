﻿using DbgCensus.Core.Objects;
using DbgCensus.Rest.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Results;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

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
            if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitKey(id), out Outfit outfit))
                return Result<Outfit?>.FromSuccess(outfit);

            Result<Outfit?> getOutfit = await base.GetOutfitAsync(id, ct).ConfigureAwait(false);

            if (getOutfit.IsDefined())
            {
                _cache.Set(
                    CacheKeyHelpers.GetOutfitKey(getOutfit.Entity),
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
            if (_cache.TryGetValue(CacheKeyHelpers.GetFacilityMapRegionKey(facilityID), out MapRegion region))
                return region;

            Result<MapRegion?> getMapRegionResult = await base.GetFacilityRegionAsync(facilityID, ct).ConfigureAwait(false);

            if (getMapRegionResult.IsDefined())
            {
                _cache.Set(
                    CacheKeyHelpers.GetFacilityMapRegionKey(getMapRegionResult.Entity),
                    getMapRegionResult.Entity,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
                        Priority = CacheItemPriority.Low
                    });
            }

            return getMapRegionResult;
        }

        ///<summary>
        ///<inheritdoc />
        /// This query is cached.
        ///</summary>
        public override async Task<Result<List<Map>>> GetMapsAsync(ValidWorldDefinition world, IEnumerable<ValidZoneDefinition> zones, CancellationToken ct = default)
        {
            List<Map> maps = new();
            List<ValidZoneDefinition> toRetrieve = new();

            foreach (ValidZoneDefinition zone in zones)
            {
                if (_cache.TryGetValue(CacheKeyHelpers.GetMapKey((WorldDefinition)world, (ZoneDefinition)zone), out Map region))
                    maps.Add(region);
                else
                    toRetrieve.Add(zone);
            }

            if (toRetrieve.Count == 0)
            {
                _logger.LogInformation("Retrieved all from cache");
                return maps;
            }

            Result<List<Map>> getMapsResult = await base.GetMapsAsync(world, toRetrieve, ct).ConfigureAwait(false);

            if (getMapsResult.IsDefined())
            {
                foreach (Map map in getMapsResult.Entity)
                {
                    _cache.Set(
                        CacheKeyHelpers.GetMapKey((WorldDefinition)world, map),
                        map,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                            Priority = CacheItemPriority.Low
                        });

                    maps.Add(map);
                }

                return maps;
            }

            return getMapsResult;
        }
    }
}
