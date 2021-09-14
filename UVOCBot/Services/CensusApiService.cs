using DbgCensus.Core.Exceptions;
using DbgCensus.Core.Objects;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Census;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    /// <inheritdoc cref="ICensusApiService"/>
    public class CensusApiService : ICensusApiService
    {
        private readonly ILogger<CensusApiService> _logger;
        private readonly IQueryService _queryService;
        private readonly IMemoryCache _cache;

        public CensusApiService(ILogger<CensusApiService> logger, IQueryService queryService, IMemoryCache cache)
        {
            _logger = logger;
            _queryService = queryService;
            _cache = cache;
        }

        // TODO: Use generic get methods where applicable?

        /// <inheritdoc />
        public async Task<Result<Outfit?>> GetOutfitAsync(string tag, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("outfit")
                .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

            try
            {
                return await _queryService.GetAsync<Outfit>(query, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get census outfit");
                return ex;
            }
        }

        /// <inheritdoc />
        /// <remarks>This query is cached.</remarks>
        public async Task<Result<Outfit?>> GetOutfitAsync(ulong id, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(GetOutfitCacheKey(id), out Outfit outfit))
                return outfit;

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("outfit")
                .Where("outfit_id", SearchModifier.Equals, id);

            try
            {
                Result<List<Outfit>?> getOutfitResult = await _queryService.GetAsync<List<Outfit>?>(query, ct).ConfigureAwait(false);

                if (getOutfitResult.IsDefined())
                {
                    outfit = getOutfitResult.Entity.Single();

                    _cache.Set(
                        GetOutfitCacheKey(id),
                        outfit,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
                            Priority = CacheItemPriority.Low
                        });
                }

                return getOutfitResult.IsSuccess
                    ? Result<Outfit?>.FromSuccess(outfit)
                    : Result<Outfit?>.FromError(getOutfitResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get census outfit");
                return ex;
            }
        }

        /// <inheritdoc/>
        public async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default)
        {
            List<ulong> outfitIds = new();

            foreach (string tag in outfitTags)
            {
                IQueryBuilder query = _queryService.CreateQuery();

                query.OnCollection("outfit")
                     .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

                Outfit? outfit = await _queryService.GetAsync<Outfit>(query, ct).ConfigureAwait(false);

                if (outfit is not null)
                    outfitIds.Add(outfit.OutfitId);
            }

            return await GetOnlineMembersAsync(outfitIds, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("outfit")
                .WhereAll("outfit_id", SearchModifier.Equals, outfitIds);

            ConstructOnlineMembersQuery(query);

            return await GetListAsync<OutfitOnlineMembers>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<NewOutfitMember>> GetNewOutfitMembersAsync(ulong outfitId, uint limit, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/outfit_member?outfit_id=37562651025751157&c:sort=member_since:-1&c:show=character_id,member_since&c:join=character%5Eshow:name.first%5Einject_at:character_name&c:limit=10

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("outfit_member")
                .Where("outfit_id", SearchModifier.Equals, outfitId)
                .WithSortOrder("member_since", SortOrder.Descending)
                .ShowFields("character_id", "member_since")
                .WithLimit(limit)
                .AddJoin("character", (j) =>
                {
                    j.ShowFields("name.first")
                        .InjectAt("character_name");
                });

            return await GetListAsync<NewOutfitMember>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<MetagameEvent>> GetMetagameEventsAsync(WorldType world, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/world_event?type=METAGAME&world_id=1&c:limit=20&c:sort=timestamp&c:join=world%5Einject_at:world

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("world_event")
                .Where("type", SearchModifier.Equals, "METAGAME")
                .Where("world_id", SearchModifier.Equals, (int)world)
                .WithLimit(20)
                .WithSortOrder("timestamp")
                .AddJoin("world", j => j.InjectAt("world"));

            return await GetListAsync<MetagameEvent>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Map>> GetMaps(WorldType world, IEnumerable<ZoneDefinition> zones, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/map?world_id=1&zone_ids=2,4,6,8

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("map")
                .Where("world_id", SearchModifier.Equals, (int)world)
                .WhereAll("zone_ids", SearchModifier.Equals, zones.Select(z => (int)z));

            return await GetListAsync<Map>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <remarks>This query is cached.</remarks>
        public async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(GetFacilityCacheKey(facilityID), out MapRegion region))
                return region;

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("map_region")
                .Where("facility_id", SearchModifier.Equals, facilityID);

            try
            {
                Result<MapRegion?> getMapRegionResult = await _queryService.GetAsync<MapRegion?>(query, ct).ConfigureAwait(false);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get census outfit");
                return ex;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "callerName filled by framework so should come last.")]
        private async Task<T> GetAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
        {
            try
            {
                T? result = await _queryService.GetAsync<T>(query, ct).ConfigureAwait(false);

                if (result is null)
                    throw new CensusException($"Census returned no data for query { callerName }.");
                else
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Census query failed for query {query}.", callerName);
                throw;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "callerName filled by framework so should come last.")]
        private async Task<List<T>> GetListAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
        {
            try
            {
                List<T>? result = await _queryService.GetAsync<List<T>>(query, ct).ConfigureAwait(false);

                if (result is null)
                    throw new CensusException($"Census returned no data for query { callerName }.");
                else
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Census query failed for query {query}.", callerName);
                throw;
            }
        }

        /// <summary>
        /// Constructs the query required to get online outfit members.
        /// </summary>
        /// <param name="preconditions">A query that has pre-prepared the filter parameters on the outfit model.</param>
        private static void ConstructOnlineMembersQuery(IQueryBuilder preconditions)
        {
            // https://census.daybreakgames.com/get/ps2/outfit?outfit_id=37562651025751157,37570391403474619&c:show=name,outfit_id,alias&c:join=outfit_member%5Einject_at:members%5Eshow:character_id%5Eouter:1%5Elist:1(character%5Eshow:name.first%5Einject_at:character%5Eouter:0%5Eon:character_id(characters_online_status%5Einject_at:online_status%5Eshow:online_status%5Eouter:0(world%5Eon:online_status%5Eto:world_id%5Eouter:0%5Eshow:world_id%5Einject_at:ignore_this))

            preconditions.ShowFields("name", "outfit_id", "alias")
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
        }

        private static object GetOutfitCacheKey(ulong outfitId)
            => (typeof(Outfit), outfitId);

        private static object GetFacilityCacheKey(ulong facilityID)
            => (typeof(MapRegion), facilityID);
    }
}
