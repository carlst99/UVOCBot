using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class ComponentInteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IServiceProvider _services;
        private readonly ContextInjectionService _contextInjectionService;

        public ComponentInteractionResponder(
            IDiscordRestInteractionAPI interactionApi,
            IServiceProvider services,
            ContextInjectionService contextInjectionService)
        {
            _interactionApi = interactionApi;
            _services = services;
            _contextInjectionService = contextInjectionService;
        }

        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.Type != InteractionType.MessageComponent)
                return Result.FromSuccess();

            if (gatewayEvent.Data.Value is null)
                return Result.FromSuccess();

            // Get the user who initiated the interaction
            IUser? user = gatewayEvent.GetUser();
            if (user is null)
                return Result.FromSuccess();

            var response = new InteractionResponse
            (
                InteractionCallbackType.DeferredChannelMessageWithSource,
                new InteractionCallbackData(Flags: InteractionCallbackDataFlags.Ephemeral)
            );

            Result createInteractionResponse = await _interactionApi.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                response,
                ct
            ).ConfigureAwait(false);

            if (!createInteractionResponse.IsSuccess)
                return createInteractionResponse;

            // Provide the created context to any services inside this scope
            Result<InteractionContext> context = gatewayEvent.ToInteractionContext();
            if (!context.IsSuccess)
                return Result.FromError(context);
            _contextInjectionService.Context = context.Entity;

            if (gatewayEvent.Data.Value.CustomID.Value is null)
                return Result.FromSuccess();

            ComponentIdFormatter.Parse(gatewayEvent.Data.Value!.CustomID.Value, out ComponentAction action, out string _);

            // Resolve the service here so that our interaction context is properly injected
            IWelcomeMessageService welcomeMessageService = _services.GetRequiredService<IWelcomeMessageService>();
            IRoleMenuService roleMenuService = _services.GetRequiredService<IRoleMenuService>();

            return action switch
            {
                ComponentAction.WelcomeMessageSetAlternate => await welcomeMessageService.SetAlternateRoles(ct).ConfigureAwait(false),
                ComponentAction.WelcomeMessageNicknameGuess => await welcomeMessageService.SetNicknameFromGuess(ct).ConfigureAwait(false),
                ComponentAction.WelcomeMessageNicknameNoMatch => await welcomeMessageService.InformNicknameNoMatch(ct).ConfigureAwait(false),
                ComponentAction.RoleMenuToggleRole => await roleMenuService.ToggleRolesAsync(ct).ConfigureAwait(false),
                ComponentAction.RoleMenuConfirmRemoveRole => await roleMenuService.ConfirmRemoveRolesAsync(ct).ConfigureAwait(false),
                _ => Result.FromSuccess()
            };
        }
    }
}
