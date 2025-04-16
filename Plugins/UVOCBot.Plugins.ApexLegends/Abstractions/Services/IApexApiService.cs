using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Abstractions.Services;

public interface IApexApiService
{
    Task<Result<IReadOnlyDictionary<string, MapRotations>>> GetMapRotationsAsync(CancellationToken ct = default);
}
