using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Objects;

namespace UVOCBot.Plugins.Planetside.Abstractions.Services;

/// <summary>
/// Represents a service for retrieving population counts.
/// </summary>
public interface IPopulationService
{
    /// <summary>
    /// Gets the population of a world.
    /// </summary>
    /// <param name="world">The world to get the population of.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <param name="skipCacheRetrieval">A value indicating whether or not to skip attempting to retrieve the population from cache.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result<IPopulation>> GetWorldPopulationAsync
    (
        ValidWorldDefinition world,
        CancellationToken ct = default,
        bool skipCacheRetrieval = false
    );
}
