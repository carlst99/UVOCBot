using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using DbgCensus.Rest.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.Services;

/// <summary>
/// <inheritdoc cref="CensusApiService" />
/// Some queries performed through this service may be cached.
/// </summary>
public class CachingCensusApiService : CensusApiService
{
    private readonly IMemoryCache _cache;

    public CachingCensusApiService
    (
        ILogger<CachingCensusApiService> logger,
        IQueryService queryService,
        IMemoryCache cache
    )
        : base(logger, queryService)
    {
        _cache = cache;
    }

    public override async Task<Result<List<Outfit>>> GetOutfitsAsync(IEnumerable<ulong> outfitIDs, CancellationToken ct = default)
    {
        List<Outfit> outfits = new();
        List<ulong> toQuery = new();

        foreach (ulong id in outfitIDs)
        {
            if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitKey(id), out Outfit outfit))
                outfits.Add(outfit);
            else
                toQuery.Add(id);
        }

        Result<List<Outfit>> outfitsResult = await base.GetOutfitsAsync(toQuery, ct);
        if (!outfitsResult.IsDefined())
            return outfitsResult;

        foreach (Outfit retrieved in outfitsResult.Entity)
        {
            _cache.Set
            (
                CacheKeyHelpers.GetOutfitKey(retrieved),
                retrieved,
                CacheEntryHelpers.OutfitOptions
            );

            outfits.Add(retrieved);
        }

        return Result<List<Outfit>>.FromSuccess(outfits);
    }

    /// <inheritdoc />
    /// <summary>
    /// This query is cached.
    /// </summary>
    public override async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetFacilityMapRegionKey(facilityID), out MapRegion region))
            return region;

        Result<MapRegion?> getMapRegionResult = await base.GetFacilityRegionAsync(facilityID, ct).ConfigureAwait(false);

        if (getMapRegionResult.IsDefined())
        {
            _cache.Set
            (
                CacheKeyHelpers.GetFacilityMapRegionKey(getMapRegionResult.Entity),
                getMapRegionResult.Entity,
                CacheEntryHelpers.MapRegionOptions
            );
        }

        return getMapRegionResult;
    }

    ///<inheritdoc />
    ///<summary>
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
            return maps;

        Result<List<Map>> getMapsResult = await base.GetMapsAsync(world, toRetrieve, ct).ConfigureAwait(false);

        if (!getMapsResult.IsDefined())
            return getMapsResult;

        foreach (Map map in getMapsResult.Entity)
        {
            _cache.Set
            (
                CacheKeyHelpers.GetMapKey((WorldDefinition)world, map),
                map,
                CacheEntryHelpers.MapOptions
            );

            maps.Add(map);
        }

        return maps;
    }

    public override async Task<Result<MetagameEvent>> GetMetagameEventAsync(ValidWorldDefinition world, ValidZoneDefinition zone, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetMetagameEventKey((WorldDefinition)world, (ZoneDefinition)zone), out MetagameEvent found))
            return found;

        // Note that we don't cache the result here
        // This is because we expect the MetagameEventResponder
        // to keep events up-to-date in a more reliable manner.
        return await base.GetMetagameEventAsync(world, zone, ct);
    }
}
