using DbgCensus.EventStream.Objects.Events.Worlds;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Abstractions.Services;

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
    /// Gets a list of outfits.
    /// </summary>
    /// <param name="outfitIDs">The outfits to query.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing the outfit list if successful.</returns>
    Task<Result<List<Outfit>>> GetOutfitsAsync(IEnumerable<ulong> outfitIDs, CancellationToken ct = default);

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

    /// <summary>
    /// Gets the most recent metagame events for a world.
    /// </summary>
    /// <param name="world">The world to query events for.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A <see cref="Result"/> representing the metagame events.</returns>
    Task<Result<List<MetagameEvent>>> GetMetagameEventsAsync
    (
        ValidWorldDefinition world,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets the most recent metagame event for a world/zone.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="zone">The zone.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing a valid <see cref="MetagameEvent"/> if successful.</returns>
    Task<Result<MetagameEvent>> GetMetagameEventAsync(ValidWorldDefinition world, ValidZoneDefinition zone, CancellationToken ct = default);

    /// <summary>
    /// Gets a collection of minimal characters.
    /// </summary>
    /// <param name="characterIDs">The ID of the characters to retrieve.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing the minimal character records if successful.</returns>
    Task<Result<List<MinimalCharacter>>> GetMinimalCharactersAsync(IEnumerable<ulong> characterIDs, CancellationToken ct = default);

    /// <summary>
    /// Gets a collection of outfit war registrations for the given world.
    /// </summary>
    /// <param name="outfitWarID">The ID of the war to retrieve registrations for.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing the outfit registrations if successful.</returns>
    Task<Result<List<OutfitWarRegistration>>> GetOutfitWarRegistrationsAsync
    (
        uint outfitWarID,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets information about the current outfit war for the given world.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>The current <see cref="OutfitWar"/>, or <c>null</c> if there is none.</returns>
    Task<Result<OutfitWar?>> GetCurrentOutfitWar
    (
        ValidWorldDefinition world,
        CancellationToken ct = default
    );
}
