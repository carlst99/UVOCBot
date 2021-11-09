using Remora.Results;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.Fisu;

namespace UVOCBot.Plugins.Planetside.Services.Abstractions;

public interface IFisuApiService
{
    /// <summary>
    /// Gets the population for a world from the fisu API.
    /// </summary>
    /// <param name="world">The world to get the population of.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A <see cref="Result{TEntity}"/> indicating the success of retrieving the <see cref="Population"/> entity.</returns>
    Task<Result<Population>> GetWorldPopulationAsync(ValidWorldDefinition world, CancellationToken ct = default);
}
