using Refit;
using System.Threading.Tasks;
using UVOCBot.Model.Planetside;

namespace UVOCBot.Services
{
    public interface IFisuApiService
    {
        [Get("/population")]
        Task<FisuPopulation> GetContinentPopulation([Query] int world);
    }
}
