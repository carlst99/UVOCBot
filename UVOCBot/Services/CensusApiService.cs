using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using Microsoft.Extensions.Logging;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Census;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class CensusApiService : ICensusApiService
    {
        private readonly ILogger<CensusApiService> _logger;
        private readonly IQueryBuilderFactory _queryFactory;
        private readonly ICensusRestClient _censusClient;

        public CensusApiService(ILogger<CensusApiService> logger, IQueryBuilderFactory censusQueryFactory, ICensusRestClient censusClient)
        {
            _logger = logger;
            _queryFactory = censusQueryFactory;
            _censusClient = censusClient;
        }

        /// <inheritdoc/>
        public async Task<Result<World>> GetWorld(WorldType world, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryFactory.Get()
                .OnCollection("world")
                .Where("world_id", SearchModifier.Equals, (int)world);

            try
            {
                World? worldResult = await _censusClient.GetAsync<World>(query, ct).ConfigureAwait(false);

                if (worldResult is null)
                    throw new Exception("No result was returned.");
                else
                    return worldResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Census world.");
                return Result<World>.FromError(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Result<OutfitOnlineMembers>> GetOnlineMembersAsync(string outfitTag, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryFactory.Get()
                .OnCollection("outfit")
                .Where("alias_lower", SearchModifier.Equals, outfitTag.ToLower());

            ConstructOnlineMembersQuery(query);

            try
            {
                OutfitOnlineMembers? onlineMembers = await _censusClient.GetAsync<OutfitOnlineMembers>(query, ct).ConfigureAwait(false);

                if (onlineMembers is null)
                    throw new Exception("No result was returned.");
                else
                    return onlineMembers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get online member status for one tag.");
                return Result<OutfitOnlineMembers>.FromError(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default)
        {
            List<ulong> outfitIds = new();

            foreach (string tag in outfitTags)
            {
                IQueryBuilder query = _queryFactory.Get();

                query.OnCollection("outfit")
                     .Where("alias_lower", SearchModifier.Equals, tag.ToLower());

                Outfit? outfit = await _censusClient.GetAsync<Outfit>(query, ct).ConfigureAwait(false);

                if (outfit is not null)
                    outfitIds.Add(outfit.OutfitId);
            }

            return await GetOnlineMembersAsync(outfitIds, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default)
        {
            IQueryBuilder query = _queryFactory.Get()
                .OnCollection("outfit")
                .Where("outfit_id", SearchModifier.Equals, outfitIds);

            ConstructOnlineMembersQuery(query);

            try
            {
                List<OutfitOnlineMembers>? onlineMembers = await _censusClient.GetAsync<List<OutfitOnlineMembers>>(query, ct).ConfigureAwait(false);

                if (onlineMembers is null)
                    throw new Exception();
                else
                    return Result<List<OutfitOnlineMembers>>.FromSuccess(onlineMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get online member status for multiple outfit IDs.");
                return Result<List<OutfitOnlineMembers>>.FromError(ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<NewOutfitMember>> GetNewOutfitMembersAsync(ulong outfitId, uint limit, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/outfit_member?outfit_id=37562651025751157&c:sort=member_since:-1&c:show=character_id,member_since&c:join=character%5Eshow:name.first%5Einject_at:character_name&c:limit=10

            IQueryBuilder query = _queryFactory.Get()
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

            try
            {
                List<NewOutfitMember>? newMembers = await _censusClient.GetAsync<List<NewOutfitMember>>(query, ct).ConfigureAwait(false);

                if (newMembers is null)
                    throw new Exception("Failed to get new outfit members");
                else
                    return newMembers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get new outfit members");
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
    }
}
