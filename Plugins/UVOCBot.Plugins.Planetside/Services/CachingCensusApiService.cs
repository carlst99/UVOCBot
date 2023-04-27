using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Services;

/// <summary>
/// <inheritdoc />
/// Some queries performed through this service may be cached.
/// </summary>
public class CachingCensusApiService : ICensusApiService
{
    private readonly ILogger<CachingCensusApiService> _logger;
    private readonly ICensusApiService _censusApiService;
    private readonly IMemoryCache _cache;

    public CachingCensusApiService
    (
        ILogger<CachingCensusApiService> logger,
        ICensusApiService censusApiService,
        IMemoryCache cache
    )
    {
        _logger = logger;
        _censusApiService = censusApiService;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<Outfit?>> GetOutfitAsync(ulong id, CancellationToken ct = default)
    {
        object cacheKey = CacheKeyHelpers.GetOutfitKey(id);
        if (_cache.TryGetValue(cacheKey, out Outfit? outfit) && outfit is not null)
            return outfit;

        Result<Outfit?> getOutfit = await _censusApiService.GetOutfitAsync(id, ct);
        if (getOutfit.IsDefined(out outfit))
            _cache.Set(cacheKey, outfit, CacheEntryHelpers.OutfitOptions);

        return getOutfit;
    }

    /// <inheritdoc />
    public async Task<Result<Outfit?>> GetOutfitAsync(string tag, CancellationToken ct = default)
    {
        object cacheKey = (typeof(Outfit), tag);
        if (_cache.TryGetValue(cacheKey, out Outfit? outfit) && outfit is not null)
            return outfit;

        Result<Outfit?> getOutfit = await _censusApiService.GetOutfitAsync(tag, ct);
        if (getOutfit.IsDefined(out outfit))
            _cache.Set(cacheKey, outfit, CacheEntryHelpers.OutfitOptions);

        return getOutfit;
    }

    /// <inheritdoc />
    public async Task<Result<List<Outfit>>> GetOutfitsAsync(IEnumerable<ulong> outfitIDs, CancellationToken ct = default)
    {
        List<Outfit> outfits = new();
        List<ulong> toQuery = new();

        foreach (ulong id in outfitIDs)
        {
            if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitKey(id), out Outfit? outfit) && outfit is not null)
                outfits.Add(outfit);
            else
                toQuery.Add(id);
        }

        Result<List<Outfit>> outfitsResult = await _censusApiService.GetOutfitsAsync(toQuery, ct);
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
    public async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync
    (
        IEnumerable<string> outfitTags,
        CancellationToken ct = default
    ) => await _censusApiService.GetOnlineMembersAsync(outfitTags, ct);

    /// <inheritdoc />
    public async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync
    (
        IEnumerable<ulong> outfitIds,
        CancellationToken ct = default
    ) => await _censusApiService.GetOnlineMembersAsync(outfitIds, ct);

    /// <inheritdoc />
    public async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetFacilityMapRegionKey(facilityID), out MapRegion? region))
            return region;

        Result<MapRegion?> getMapRegionResult = await _censusApiService.GetFacilityRegionAsync(facilityID, ct).ConfigureAwait(false);

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
    public async Task<Result<List<Map>>> GetMapsAsync
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
            if (_cache.TryGetValue(CacheKeyHelpers.GetMapKey((WorldDefinition)world, (ZoneDefinition)zone), out Map? region))
                maps.Add(region!);
            else
                toRetrieve.Add(zone);
        }

        if (toRetrieve.Count == 0)
            return maps;

        _logger.LogWarning("Couldn't retrieve maps for {World} from cache! {Maps}", world, toRetrieve);
        Result<List<Map>> getMapsResult = await _censusApiService.GetMapsAsync(world, toRetrieve, ct).ConfigureAwait(false);

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
    public async Task<Result<MetagameEvent>> GetMetagameEventAsync
    (
        ValidWorldDefinition world,
        ValidZoneDefinition zone,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetMetagameEventKey((WorldDefinition)world, (ZoneDefinition)zone), out MetagameEvent? found))
            return found;

        // Note that we don't cache the result here
        // This is because we expect the MetagameEventResponder
        // to keep events up-to-date in a more reliable manner.
        return await _censusApiService.GetMetagameEventAsync(world, zone, ct);
    }

    /// <inheritdoc />
    public async Task<Result<List<MetagameEvent>>> GetMetagameEventsAsync
    (
        ValidWorldDefinition world,
        CancellationToken ct = default
    ) => await _censusApiService.GetMetagameEventsAsync(world, ct);

    public async Task<Result<List<MinimalCharacter>>> GetMinimalCharactersAsync
    (
        IEnumerable<ulong> characterIDs,
        CancellationToken ct = default
    )
    {
        List<MinimalCharacter> characters = new();
        List<ulong> toQuery = new();

        foreach (ulong id in characterIDs)
        {
            if (_cache.TryGetValue(CacheKeyHelpers.GetMinimalCharacterKey(id), out MinimalCharacter? character))
                characters.Add(character!);
            else
                toQuery.Add(id);
        }

        Result<List<MinimalCharacter>> retrieveResult = await _censusApiService.GetMinimalCharactersAsync(toQuery, ct).ConfigureAwait(false);
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

    public async Task<Result<List<OutfitWarRegistration>>> GetOutfitWarRegistrationsAsync
    (
        uint outfitWarID,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitWarRegistrationsKey(outfitWarID), out List<OutfitWarRegistration>? registrations))
            return registrations;

        Result<List<OutfitWarRegistration>> getRegistrations = await _censusApiService.GetOutfitWarRegistrationsAsync(outfitWarID, ct)
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

    public async Task<Result<OutfitWar?>> GetCurrentOutfitWar
    (
        ValidWorldDefinition world,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitWarKey(world), out OutfitWar? war))
            return war;

        Result<OutfitWar?> getWar = await _censusApiService.GetCurrentOutfitWar(world, ct).ConfigureAwait(false);

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

    public async Task<Result<OutfitWarRoundWithMatches?>> GetCurrentOutfitWarMatches
    (
        uint outfitWarID,
        CancellationToken ct = default
    )
    {
        if (_cache.TryGetValue(CacheKeyHelpers.GetOutfitWarRoundWithMatchesKey(outfitWarID), out OutfitWarRoundWithMatches? round))
            return round;

        Result<OutfitWarRoundWithMatches?> getRound = await _censusApiService.GetCurrentOutfitWarMatches(outfitWarID, ct)
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

    /// <inheritdoc />
    public async Task<Result<ExperienceRank?>> GetExperienceRankAsync
    (
        int rank,
        int prestigeLevel,
        CancellationToken ct = default
    )
    {
        object cacheKey = CacheKeyHelpers.GetExperienceRankKey(rank, prestigeLevel);
        if (_cache.TryGetValue(cacheKey, out ExperienceRank? expRank) && expRank is not null)
            return expRank;

        Result<ExperienceRank?> getRank = await _censusApiService.GetExperienceRankAsync(rank, prestigeLevel, ct);
        if (getRank.IsDefined(out expRank))
            _cache.Set(cacheKey, expRank, CacheEntryHelpers.GetExperienceRankOptions());

        return getRank;
    }
}
