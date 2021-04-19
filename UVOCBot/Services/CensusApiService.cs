using DaybreakGames.Census;
using DaybreakGames.Census.Operators;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Planetside;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class CensusApiService : ICensusApiService
    {
        private readonly ICensusQueryFactory _queryFactory;

        public CensusApiService(ICensusQueryFactory censusQueryFactory)
        {
            _queryFactory = censusQueryFactory;
        }

        /// <inheritdoc/>
        public async Task<Result<World>> GetWorld(WorldType world, CancellationToken ct = default)
        {
            var query = _queryFactory.Create("world").SetLanguage(CensusLanguage.English);
            query.Where("world_id").Equals((int)world);

            try
            {
                return await query.GetAsync<World>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Result<World>.FromError(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Result<OutfitOnlineMembers>> GetOnlineMembersAsync(string outfitTag, CancellationToken ct = default)
        {
            CensusQuery query = _queryFactory.Create("outfit");
            query.Where("alias_lower").Equals(outfitTag.ToLower());

            ConstructOnlineMembersQuery(query);

            try
            {
                return await query.GetAsync<OutfitOnlineMembers>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Result<OutfitOnlineMembers>.FromError(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default)
        {
            List<ulong> outfitIds = new();

            foreach (string tag in outfitTags)
            {
                CensusQuery query = _queryFactory.Create("outfit");
                query.Where("alias_lower").Equals(tag.ToLower());

                Outfit outfit = await query.GetAsync<Outfit>().ConfigureAwait(false);
                outfitIds.Add(outfit.OutfitId);
            }

            return await GetOnlineMembersAsync(outfitIds, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default)
        {
            // https://census.daybreakgames.com/get/ps2/outfit?outfit_id=37562651025751157,37570391403474619&c:show=name,outfit_id,alias&c:join=outfit_member%5Einject_at:members%5Eshow:character_id%5Eouter:0%5Elist:1(character%5Eshow:name.first%5Einject_at:character%5Eouter:0%5Eon:character_id(characters_online_status%5Einject_at:online_status%5Eshow:online_status%5Eouter:0(world%5Eon:online_status%5Eto:world_id%5Eouter:0%5Eshow:world_id%5Einject_at:ignore_this))

            CensusQuery query = _queryFactory.Create("outfit");

            string idsToQuery = string.Empty;
            foreach (ulong id in outfitIds)
                idsToQuery += id.ToString() + ",";
            idsToQuery = idsToQuery.TrimEnd(',');

            query.Where("outfit_id").Equals(idsToQuery);
            query.ShowFields("name", "outfit_id", "alias");

            ConstructOnlineMembersQuery(query);

            IEnumerable<OutfitOnlineMembers> onlineMembers;
            try
            {
                onlineMembers = await query.GetListAsync<OutfitOnlineMembers>().WithCancellation(ct).ConfigureAwait(false);
                return Result<IEnumerable<OutfitOnlineMembers>>.FromSuccess(onlineMembers);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<OutfitOnlineMembers>>.FromError(ex);
            }
        }

        /// <summary>
        /// Constructs the query required to get online outfit members.
        /// </summary>
        /// <param name="preconditions">A query that has pre-prepared the filter parameters on the outfit model.</param>
        private static void ConstructOnlineMembersQuery(CensusQuery preconditions)
        {
            CensusJoin outfitMemberJoin = preconditions.JoinService("outfit_member");
            outfitMemberJoin.WithInjectAt("members");
            outfitMemberJoin.ShowFields("character_id");
            outfitMemberJoin.IsOuterJoin(false);
            outfitMemberJoin.IsList(true);

            CensusJoin characterJoin = outfitMemberJoin.JoinService("character");
            characterJoin.OnField("character_id");
            characterJoin.WithInjectAt("character");
            characterJoin.ShowFields("name.first");
            characterJoin.IsOuterJoin(false);

            CensusJoin onlineStatusJoin = characterJoin.JoinService("characters_online_status");
            onlineStatusJoin.WithInjectAt("online_status");
            onlineStatusJoin.ShowFields("online_status");
            onlineStatusJoin.IsOuterJoin(false);

            CensusJoin worldJoin = onlineStatusJoin.JoinService("world");
            worldJoin.OnField("online_status");
            worldJoin.ToField("world_id");
            worldJoin.WithInjectAt("ignore_this");
            worldJoin.IsOuterJoin(false);
            worldJoin.ShowFields("world_id");
        }
    }
}
