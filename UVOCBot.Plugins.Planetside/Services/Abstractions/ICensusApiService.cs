using Remora.Results;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.Census;

namespace UVOCBot.Plugins.Planetside.Services.Abstractions
{
    /// <summary>
    /// Provides methods to retrieve data from the Census query API.
    /// </summary>
    public interface ICensusApiService
    {
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
        Task<Result<List<Map>>> GetMaps(ValidWorldDefinition world, IEnumerable<ValidZoneDefinition> zones, CancellationToken ct = default);

        /// <summary>
        /// Gets a facility.
        /// </summary>
        /// <param name="facilityID">The facility.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A <see cref="Result"/> representing the facility.</returns>
        Task<Result<MapRegion?>> GetFacilityRegionAsync(ulong facilityID, CancellationToken ct = default);
    }
}
