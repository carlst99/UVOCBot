using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Census;

namespace UVOCBot.Services.Abstractions
{
    public interface ICensusApiService
    {
        /// <summary>
        /// Gets an outfit.
        /// </summary>
        /// <param name="tag">The outfit tag.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns></returns>
        Task<Result<Outfit?>> GetOutfit(string tag, CancellationToken ct = default);

        /// <summary>
        /// Gets information about a world.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="ct">A token which can be used to cancel asynchronous logic.</param>
        /// <returns></returns>
        Task<Result<World>> GetWorld(WorldType world, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of an outfit.
        /// </summary>
        /// <param name="outfitTag">The case-insensitive tag of the outfit.</param>
        /// <param name="ct">A token which can be used to cancel asynchronous logic.</param>
        /// <returns></returns>
        Task<Result<OutfitOnlineMembers>> GetOnlineMembersAsync(string outfitTag, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of a number of outfits.
        /// </summary>
        /// <param name="outfitTags">The case-insensitive tags of the outfit.</param>
        /// <param name="ct">A token which can be used to cancel asynchronous logic.</param>
        /// <returns></returns>
        Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of a number of outfit.
        /// </summary>
        /// <param name="outfitIds">The IDs of the outfits.</param>
        /// <param name="ct">A token which can be used to cancel asynchronous logic.</param>
        /// <returns></returns>
        Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default);

        /// <summary>
        /// Gets new members of an outfit.
        /// </summary>
        /// <param name="outfitId">The ID of the outfit.</param>
        /// <param name="limit">The number of new members to get.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> to stop the operation with.</param>
        /// <returns></returns>
        Task<List<NewOutfitMember>> GetNewOutfitMembersAsync(ulong outfitId, uint limit, CancellationToken ct = default);

        /// <summary>
        /// Gets the most recent metagame events for a world.
        /// </summary>
        /// <param name="world">The world to retrieve events for.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A list of metagame events.</returns>
        Task<List<MetagameEvent>> GetMetagameEventsAsync(WorldType world, CancellationToken ct = default);

        /// <summary>
        /// Gets the maps for a world.
        /// </summary>
        /// <param name="world">The world to retrieve the maps for.</param>
        /// <param name="zones">The zones to retrieve maps for.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A list of maps.</returns>
        Task<List<Map>> GetMaps(WorldType world, IEnumerable<ZoneType> zones, CancellationToken ct = default);
    }
}
