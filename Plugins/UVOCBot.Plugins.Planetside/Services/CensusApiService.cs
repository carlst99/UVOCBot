using DbgCensus.Core.Exceptions;
using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="ICensusApiService"/>
public class CensusApiService : ICensusApiService, IDisposable
{
    /// <summary>
    /// Gets the number of metagame events that are expected to be
    /// received over the course of ALL continents opening and closing.
    /// </summary>
    /// <remarks>
    /// Currently, a start and end event for each continent is expected.
    /// Also, adding 2 for resiliency.
    /// Hence, 2 * VALID_CONT_COUNT + 2.
    /// </remarks>
    public const int ExpectedMetagameEventsPerFullCycle = 12;

    private readonly SemaphoreSlim _queryLimiter;

    protected readonly ILogger<CensusApiService> _logger;
    protected readonly IQueryService _queryService;

    private bool _isDisposed;

    public CensusApiService(ILogger<CensusApiService> logger, IQueryService queryService)
    {
        _logger = logger;
        _queryService = queryService;
        _queryLimiter = new SemaphoreSlim(8, 8);
    }

    /// <inheritdoc />
    public virtual async Task<Result<Outfit?>> GetOutfitAsync(string tag, CancellationToken ct = default)
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("outfit")
            .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

        return await GetAsync<Outfit>(query, ct).ConfigureAwait(false);
    }

    public virtual async Task<Result<List<Outfit>>> GetOutfitsAsync(IEnumerable<ulong> outfitIDs, CancellationToken ct = default)
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("outfit")
            .WhereAll("outfit_id", SearchModifier.Equals, outfitIDs);

        return await GetListAsync<Outfit>(query, ct);
    }

    /// <inheritdoc />
    public virtual async Task<Result<Outfit?>> GetOutfitAsync(ulong id, CancellationToken ct = default)
    {
        Result<List<Outfit>> outfitsResult = await GetOutfitsAsync(new[] { id }, ct);
        if (!outfitsResult.IsSuccess)
            return Result<Outfit?>.FromError(outfitsResult);

        return outfitsResult.Entity.Count == 0
            ? default
            : outfitsResult.Entity[0];
    }

    /// <inheritdoc/>
    public virtual async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default)
    {
        List<ulong> outfitIds = new();

        await Parallel.ForEachAsync
        (
            outfitTags,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 3 },
            async (tag, ict) =>
            {
                IQueryBuilder query = _queryService.CreateQuery();

                query.OnCollection("outfit")
                     .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

                Result<Outfit?> outfitResult = await GetAsync<Outfit>(query, ict);
                if (!outfitResult.IsDefined(out Outfit? outfit))
                {
                    _logger.LogError("Failed to retrieve outfit: {Error}", outfitResult.Error);
                    return;
                }

                outfitIds.Add(outfit.OutfitId);
            }
        ).ConfigureAwait(false);

        if (outfitIds.Count == 0)
            return new List<OutfitOnlineMembers>();

        return await GetOnlineMembersAsync(outfitIds, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default)
    {
        // https://census.daybreakgames.com/get/ps2/outfit?outfit_id=37562651025751157,37570391403474619&c:show=name,outfit_id,alias&c:join=outfit_member%5Einject_at:members%5Eshow:character_id%5Eouter:1%5Elist:1(character%5Eshow:name.first%5Einject_at:character%5Eouter:0%5Eon:character_id(characters_online_status%5Einject_at:online_status%5Eshow:online_status%5Eouter:0(world%5Eon:online_status%5Eto:world_id%5Eouter:0%5Eshow:world_id%5Einject_at:ignore_this))

        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("outfit")
            .WhereAll("outfit_id", SearchModifier.Equals, outfitIds);

        query.ShowFields("name", "outfit_id", "alias")
            .AddJoin("outfit_member")
                .InjectAt("members")
                .ShowFields("character_id")
                .IsList()
                .AddNestedJoin("character")
                    .OnField("character_id")
                    .InjectAt("character")
                    .ShowFields("name.first")
                    .IsInnerJoin()
                    .AddNestedJoin("characters_online_status")
                        .InjectAt("online_status")
                        .ShowFields("online_status")
                        .IsInnerJoin()
                        .AddNestedJoin("world")
                            .OnField("online_status")
                            .ToField("world_id")
                            .InjectAt("ignore_this")
                            .ShowFields("world_id")
                            .IsInnerJoin();

        return await GetListAsync<OutfitOnlineMembers>(query, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("map_region")
            .Where("facility_id", SearchModifier.Equals, facilityID);

        return await GetAsync<MapRegion>(query, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<Result<List<Map>>> GetMapsAsync
    (
        ValidWorldDefinition world,
        IEnumerable<ValidZoneDefinition> zones,
        CancellationToken ct = default
    )
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("map")
            .Where("world_id", SearchModifier.Equals, (int)world)
            .WhereAll("zone_ids", SearchModifier.Equals, zones.Cast<ushort>());

        return await GetListAsync<Map>(query, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<Result<List<MetagameEvent>>> GetMetagameEventsAsync
    (
        ValidWorldDefinition world,
        int limit = ExpectedMetagameEventsPerFullCycle,
        CancellationToken ct = default
    )
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("world_event")
            .Where("type", SearchModifier.Equals, "METAGAME")
            .Where("world_id", SearchModifier.Equals, (int)world)
            .WithLimit(limit)
            .WithSortOrder("timestamp");

        return await GetListAsync<MetagameEvent>(query, ct).ConfigureAwait(false);
    }

    public virtual async Task<Result<MetagameEvent>> GetMetagameEventAsync
    (
        ValidWorldDefinition world,
        ValidZoneDefinition zone,
        CancellationToken ct = default
    )
    {
        Result<List<MetagameEvent>> eventsResult = await GetMetagameEventsAsync(world, ct: ct);
        if (!eventsResult.IsDefined(out List<MetagameEvent>? events))
            return Result<MetagameEvent>.FromError(eventsResult);

        foreach (MetagameEvent mev in events)
        {
            if (mev.ZoneID.Definition == (ZoneDefinition)zone)
                return mev;
        }

        return Result<MetagameEvent>.FromError(new CensusException("Census did not provide enough data."));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task<Result<T?>> GetAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
    {
        bool enteredSemaphore = false;

        try
        {
            enteredSemaphore = await _queryLimiter.WaitAsync(5000, ct);
            if (!enteredSemaphore)
            {
                _logger.LogError("Failed to enter query semaphore on route {Caller}", callerName);
                return new TimeoutException("Failed to enter query semaphore on route " + callerName);
            }

            T? result = await _queryService.GetAsync<T>(query, ct);

            return result;
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            _logger.LogError(ex, "Census query failed for query {Query}", callerName);
            return ex;
        }
        finally
        {
            if (enteredSemaphore)
                _queryLimiter.Release();
        }
    }

    protected async Task<Result<List<T>>> GetListAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
    {
        Result<List<T>?> result = await GetAsync<List<T>>(query, ct, callerName);
        if (!result.IsSuccess)
            return Result<List<T>>.FromError(result);

        return result.Entity is null
            ? new CensusException($"Census returned no data for query {callerName}.")
            : result.Entity;
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="disposeManaged">A value indicating whether or not to dispose of managed resources.</param>
    protected virtual void Dispose(bool disposeManaged)
    {
        if (_isDisposed)
            return;

        if (disposeManaged)
            _queryLimiter.Dispose();

        _isDisposed = true;
    }
}
