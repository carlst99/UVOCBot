using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Abstractions.Services;

public interface IApexApiService
{
    Task<Result<MapRotationBundle>> GetMapRotationsAsync(CancellationToken ct = default);
}
