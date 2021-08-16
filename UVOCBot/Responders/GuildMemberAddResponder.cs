using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class GuildMemberAddResponder : IResponder<IGuildMemberAdd>
    {
        private readonly IServiceProvider _services;
        private readonly ContextInjectionService _contextInjectionService;

        public GuildMemberAddResponder(
            IServiceProvider services,
            ContextInjectionService contextInjectionService)
        {
            _services = services;
            _contextInjectionService = contextInjectionService;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.User.HasValue || gatewayEvent.User.Value is null)
                return Result.FromSuccess();

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
    }
}
