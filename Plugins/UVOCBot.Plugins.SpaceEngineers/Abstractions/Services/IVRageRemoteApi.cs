using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;

public interface IVRageRemoteApi
{
    Task<Result<bool>> PingAsync(CancellationToken ct = default);
}
