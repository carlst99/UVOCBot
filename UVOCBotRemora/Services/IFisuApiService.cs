using Refit;
using System.Threading.Tasks;
using UVOCBotRemora.Model.Planetside;

namespace UVOCBotRemora.Services
{
    public interface IFisuApiService
    {
        [Get("/population")]
        Task<FisuPopulation> GetContinentPopulation([Query] int world);
    }
}
