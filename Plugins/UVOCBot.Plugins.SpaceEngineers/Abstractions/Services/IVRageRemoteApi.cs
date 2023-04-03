using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

namespace UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;

public interface IVRageRemoteApi
{
    Task<Result<bool>> PingAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<Player>>> GetPlayersAsync(CancellationToken ct = default);
}
