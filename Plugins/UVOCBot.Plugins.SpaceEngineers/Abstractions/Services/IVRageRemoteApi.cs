using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.SpaceEngineers.Objects;
using UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

namespace UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;

public interface IVRageRemoteApi
{
    Task<Result<bool>> PingAsync(SEServerConnectionDetails connectionDetails, CancellationToken ct = default);

    Task<Result<IReadOnlyList<Player>>> GetPlayersAsync
    (
        SEServerConnectionDetails connectionDetails,
        CancellationToken ct = default
    );
}
