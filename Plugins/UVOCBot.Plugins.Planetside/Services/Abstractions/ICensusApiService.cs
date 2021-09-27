using Remora.Results;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.Census;
using UVOCBot.Plugins.Planetside.Objects.Census.Map;
using UVOCBot.Plugins.Planetside.Objects.Census.Outfit;

namespace UVOCBot.Plugins.Planetside.Services.Abstractions
{
    /// <summary>
    /// Provides methods to retrieve data from the Census query API.
    /// </summary>
    public interface ICensusApiService
    {
        /// <summary>
        /// Gets an outfit.
        /// </summary>
        /// <param name="tag">The outfit tag.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A <see cref="Result"/> representing the <see cref="Outfit"/>, or <c>null</c> if the outfit does not exist.</returns>
        Task<Result<Outfit?>> GetOutfitAsync(string tag, CancellationToken ct = default);

        /// <summary>
        /// Gets an outfit.
        /// </summary>
        /// <param name="id">The ID of the outfit.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A <see cref="Result"/> representing the <see cref="Outfit"/>, or <c>null</c> if the outfit does not exist.</returns>
        Task<Result<Outfit?>> GetOutfitAsync(ulong id, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of a number of outfits.
        /// </summary>
        /// <param name="outfitTags">The case-insensitive tags of the outfit.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A list of online members.</returns>
        Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<string> outfitTags, CancellationToken ct = default);

        /// <summary>
        /// Gets the online members of a number of outfit.
        /// </summary>
        /// <param name="outfitIds">The IDs of the outfits.</param>
        /// <param name="ct">A token which can be used to cancel asynchronous logic.</param>
        /// <returns>A list of online outfit members for each outfit in the query.</returns>
        Task<Result<List<OutfitOnlineMembers>>> GetOnlineMembersAsync(IEnumerable<ulong> outfitIds, CancellationToken ct = default);

        /// <summary>
        /// Gets new members of an outfit.
        /// </summary>
        /// <param name="outfitId">The ID of the outfit.</param>
        /// <param name="limit">The number of new members to get.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A list of the new outfit members.</returns>
        Task<Result<List<NewOutfitMember>>> GetNewOutfitMembersAsync(ulong outfitId, uint limit, CancellationToken ct = default);

        /// <summary>
        /// Gets the most recent metagame events for a world.
        /// </summary>
        /// <param name="world">The world to retrieve events for.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A list of metagame events.</returns>
        Task<Result<List<MetagameEvent>>> GetMetagameEventsAsync(ValidWorldDefinition world, CancellationToken ct = default);

        /// <summary>
        /// Gets the maps for a world.
        /// </summary>
        /// <param name="world">The world to retrieve the maps for.</param>
        /// <param name="zones">The zones to retrieve maps for.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A list of maps.</returns>
        Task<Result<List<Map>>> GetMapsAsync(ValidWorldDefinition world, IEnumerable<ValidZoneDefinition> zones, CancellationToken ct = default);

        /// <summary>
        /// Gets a facility.
        /// </summary>
        /// <param name="facilityID">The facility.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A <see cref="Result"/> representing the <see cref="MapRegion"/>, or <c>null</c> if the facility does not exist.</returns>
        Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default);
    }
}
