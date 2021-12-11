using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders;

public class GuildMemberResponder : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberRemove>
{
    private readonly ILogger<GuildMemberResponder> _logger;
    private readonly IServiceProvider _services;
    private readonly IAdminLogService _adminLogService;
    private readonly ContextInjectionService _contextInjectionService;

    public GuildMemberResponder(
        ILogger<GuildMemberResponder> logger,
        IServiceProvider services,
        IAdminLogService adminLogService,
        ContextInjectionService contextInjectionService)
    {
        _logger = logger;
        _services = services;
        _adminLogService = adminLogService;
        _contextInjectionService = contextInjectionService;
    }

    public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
    {
        if (!gatewayEvent.User.HasValue || gatewayEvent.User.Value is null)
            return Result.FromSuccess();

        await _adminLogService.LogMemberJoin(gatewayEvent, ct).ConfigureAwait(false);

        // Yep, this is a bit naughty
        // The welcome message service is scoped, and expects a context
        // So we make a fake one.
        _contextInjectionService.Context = new InteractionContext(
            gatewayEvent.GuildID,
            new Snowflake(),
            gatewayEvent.User.Value,
            default,
            string.Empty,
            new Snowflake(),
            new Snowflake(),
            new InteractionData(default, default, default));

        // Resolve the welcome message service here so that the context is properly injected
        return await _services.GetRequiredService<IWelcomeMessageService>().SendWelcomeMessage(gatewayEvent, ct).ConfigureAwait(false);
    }

    public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = default)
    {
        Result res = await _adminLogService.LogMemberLeave(gatewayEvent, ct).ConfigureAwait(false);
        if (!res.IsSuccess)
            _logger.LogError("Failed to admin-log a member remove event: {error}", res.Error);

        return res;
    }
}
