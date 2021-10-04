using DbgCensus.Core.Exceptions;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System.Runtime.CompilerServices;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Services
{
    /// <inheritdoc cref="ICensusApiService"/>
    public class CensusApiService : ICensusApiService
    {
        protected readonly ILogger<CensusApiService> _logger;
        protected readonly IQueryService _queryService;

        public CensusApiService(ILogger<CensusApiService> logger, IQueryService queryService)
        {
            _logger = logger;
            _queryService = queryService;
        }

        /// <inheritdoc />
        public virtual async Task<Result<Outfit?>> GetOutfitAsync(string tag, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("outfit")
                .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

            return await GetAsync<Outfit?>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <remarks>This query is cached.</remarks>
        public virtual async Task<Result<Outfit?>> GetOutfitAsync(ulong id, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("outfit")
                .Where("outfit_id", SearchModifier.Equals, id);

            return await GetAsync<Outfit?>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default)
        {
            List<ulong> outfitIds = new();

            await Parallel.ForEachAsync(
                outfitTags,
                new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 3 },
                async (tag, ct) =>
                {
                    IQueryBuilder query = _queryService.CreateQuery();

                    query.OnCollection("outfit")
                         .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

                    Outfit? outfit = await _queryService.GetAsync<Outfit?>(query, ct).ConfigureAwait(false);

                    if (outfit is not null)
                        outfitIds.Add(outfit.OutfitId);
                }
            ).ConfigureAwait(false);

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
        public async Task<Result<List<NewOutfitMember>>> GetNewOutfitMembersAsync(ulong outfitId, uint limit, CancellationToken ct = default)
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
        public virtual async Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("map_region")
                .Where("facility_id", SearchModifier.Equals, facilityID);

            return await GetAsync<MapRegion>(query, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual async Task<Result<List<Map>>> GetMapsAsync(ValidWorldDefinition world, IEnumerable<ValidZoneDefinition> zones, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/map?world_id=1&zone_ids=2,4,6,8

            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("map")
                .Where("world_id", SearchModifier.Equals, (int)world)
                .WhereAll("zone_ids", SearchModifier.Equals, zones.Select(z => (int)z));

            return await GetListAsync<Map>(query, ct).ConfigureAwait(false);
        }

        public async Task<Result<List<QueryMetagameEvent>>> GetMetagameEventsAsync(ValidWorldDefinition world, uint limit = 10, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryService.CreateQuery()
                .OnCollection("world_event")
                .Where("type", SearchModifier.Equals, "METAGAME")
                .Where("world_id", SearchModifier.Equals, (int)world)
                .WithLimit(limit)
                .WithSortOrder("timestamp");

            return await GetListAsync<QueryMetagameEvent>(query, ct).ConfigureAwait(false);
        }

        protected async Task<Result<T?>> GetAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
        {
            try
            {
                return await _queryService.GetAsync<T>(query, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Census query failed for query {query}.", callerName);
                return ex;
            }
        }

        protected async Task<Result<List<T>>> GetListAsync<T>(IQueryBuilder query, CancellationToken ct = default, [CallerMemberName] string? callerName = null)
        {
            try
            {
                List<T>? result = await _queryService.GetAsync<List<T>>(query, ct).ConfigureAwait(false);

                if (result is null)
                    return new CensusException($"Census returned no data for query { callerName }.");
                else
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Census query failed for query {query}.", callerName);
                return ex;
            }
        }
    }
}
