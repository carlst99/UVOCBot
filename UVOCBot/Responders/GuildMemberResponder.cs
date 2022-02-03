using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders;

public class GuildMemberResponder : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberRemove>
{
    private readonly ILogger<GuildMemberResponder> _logger;
    private readonly IAdminLogService _adminLogService;

    public GuildMemberResponder
    (
        ILogger<GuildMemberResponder> logger,
        IAdminLogService adminLogService
    )
    {
        _logger = logger;
        _adminLogService = adminLogService;
    }

    public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
    {
        Result res = await _adminLogService.LogMemberJoin(gatewayEvent, ct).ConfigureAwait(false);
        if (!res.IsSuccess)
            _logger.LogError("Failed to admin-log a member remove event: {error}", res.Error);

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = default)
    {
        Result res = await _adminLogService.LogMemberLeave(gatewayEvent, ct).ConfigureAwait(false);
        if (!res.IsSuccess)
            _logger.LogError("Failed to admin-log a member remove event: {error}", res.Error);

        return Result.FromSuccess();
    }
}
