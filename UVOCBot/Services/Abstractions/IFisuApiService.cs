using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Census;

namespace UVOCBot.Services.Abstractions
{
    public interface IFisuApiService
    {
        /// <summary>
        /// Gets the population for a world from the fisu API
        /// </summary>
        /// <param name="world">The world to get the population of</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<FisuPopulation>> GetContinentPopulationAsync(WorldType world, CancellationToken ct = default);
    }
}
