using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Abstractions.Services;

public interface IApexApiService
{
    Task<Result<MapRotationBundle>> GetMapRotationsAsync(CancellationToken ct = default);
    Task<Result<List<CraftingBundle>>> GetCraftingBundlesAsync(CancellationToken ct = default);
    Task<Result<StatsBridge>> GetPlayerStatisticsAsync(string playerName, PlayerPlatform platform, CancellationToken ct = default);
}
