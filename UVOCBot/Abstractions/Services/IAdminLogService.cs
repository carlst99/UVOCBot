using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Abstractions.Services;

public interface IAdminLogService
{
    Task<Result> LogMemberJoin(IGuildMemberAdd member, CancellationToken ct = default);
    Task<Result> LogMemberLeave(IGuildMemberRemove user, CancellationToken ct = default);
}
