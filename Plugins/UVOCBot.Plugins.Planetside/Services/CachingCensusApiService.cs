using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using DbgCensus.Rest;
using DbgCensus.Rest.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

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
        IOptionsMonitor<CensusQueryOptions> queryOptions,
        IMemoryCache cache
    ) : base(logger, queryService, queryOptions)
    {
        _cache = cache;
    }

    /// <inheritdoc />
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
    public override async Task<Result<List<Map>>> GetMapsAsync
    (
        ValidWorldDefinition world,
        IEnumerable<ValidZoneDefinition> zones,
        CancellationToken ct = default
    )
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

        _logger.LogWarning("Couldn't retrieve maps for {World} from cache! {Maps}", world, toRetrieve);
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

    /// <inheritdoc />
    public override async Task<Result<MetagameEvent>> GetMetagameEventAsync(ValidWorldDefinition world, ValidZoneDefinition zone, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetMetagameEventKey((WorldDefinition)world, (ZoneDefinition)zone), out MetagameEvent found))
            return found;

        // Note that we don't cache the result here
        // This is because we expect the MetagameEventResponder
        // to keep events up-to-date in a more reliable manner.
        return await base.GetMetagameEventAsync(world, zone, ct);
    }

    public override async Task<Result<List<MinimalCharacter>>> GetMinimalCharactersAsync
    (
        IEnumerable<ulong> characterIDs,
        CancellationToken ct = default
    )
    {
        List<MinimalCharacter> characters = new();
        List<ulong> toQuery = new();

        foreach (ulong id in characterIDs)
        {
            if (_cache.TryGetValue(CacheKeyHelpers.GetMinimalCharacterKey(id), out MinimalCharacter character))
                characters.Add(character);
            else
                toQuery.Add(id);
        }

        Result<List<MinimalCharacter>> retrieveResult = await base.GetMinimalCharactersAsync(toQuery, ct).ConfigureAwait(false);
        if (!retrieveResult.IsSuccess)
            return retrieveResult;

        foreach (MinimalCharacter rc in retrieveResult.Entity)
        {
            _cache.Set
            (
                CacheKeyHelpers.GetMinimalCharacterKey(rc),
                rc,
                CacheEntryHelpers.MinimalCharacterOptions
            );

            characters.Add(rc);
        }

        return Result<List<MinimalCharacter>>.FromSuccess(characters);
    }

    public override async Task<Result<List<OutfitWarRegistration>>> GetOutfitWarRegistrationsAsync
    (
        uint outfitWarID,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitWarRegistrationsKey(outfitWarID), out List<OutfitWarRegistration> registrations))
            return registrations;

        Result<List<OutfitWarRegistration>> getRegistrations = await base.GetOutfitWarRegistrationsAsync(outfitWarID, ct)
            .ConfigureAwait(false);

        if (getRegistrations.IsDefined())
        {
            _cache.Set
            (
                CacheKeyHelpers.GetOutfitWarRegistrationsKey(outfitWarID),
                getRegistrations.Entity,
                CacheEntryHelpers.OutfitWarRegistrationsOptions
            );
        }

        return getRegistrations;
    }

    public override async Task<Result<OutfitWar?>> GetCurrentOutfitWar
    (
        ValidWorldDefinition world,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitWarKey(world), out OutfitWar war))
            return war;

        Result<OutfitWar?> getWar = await base.GetCurrentOutfitWar(world, ct).ConfigureAwait(false);

        if (getWar.IsDefined())
        {
            _cache.Set
            (
                CacheKeyHelpers.GetOutfitWarKey(world),
                getWar.Entity,
                CacheEntryHelpers.OutfitWarOptions
            );
        }

        return getWar;
    }

    public override async Task<Result<OutfitWarRoundWithMatches?>> GetCurrentOutfitWarMatches
    (
        uint outfitWarID,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitWarRoundWithMatchesKey(outfitWarID), out OutfitWarRoundWithMatches round))
            return round;

        Result<OutfitWarRoundWithMatches?> getRound = await base.GetCurrentOutfitWarMatches(outfitWarID, ct)
            .ConfigureAwait(false);

        if (getRound.IsDefined())
        {
            _cache.Set
            (
                CacheKeyHelpers.GetOutfitWarRoundWithMatchesKey(outfitWarID),
                getRound.Entity,
                CacheEntryHelpers.GetOutfitWarRoundWithMatchesOptions(getRound.Entity)
            );
        }

        return getRound;
    }
}
