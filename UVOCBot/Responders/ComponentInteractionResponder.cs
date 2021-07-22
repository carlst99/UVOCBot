using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Extensions;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class ComponentInteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IWelcomeMessageService _welcomeMessageService;
        private readonly ContextInjectionService _contextInjectionService;

        public ComponentInteractionResponder(
            IDiscordRestInteractionAPI interactionApi,
            IWelcomeMessageService welcomeMessageService,
            ContextInjectionService contextInjectionService)
        {
            _interactionApi = interactionApi;
            _welcomeMessageService = welcomeMessageService;
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
                new InteractionApplicationCommandCallbackData(Flags: MessageFlags.Ephemeral)
            );

            // Signal to Discord that we'll be handling this one asynchronously
            // We're not awaiting this, so that the command processing begins ASAP
            // This can cause some wacky user-side behaviour if Discord doesn't process the interaction response in time
            Task<Result> createInteractionResponse = _interactionApi.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                response,
                ct
            );

            // Provide the created context to any services inside this scope
            Result<InteractionContext> context = gatewayEvent.ToInteractionContext();
            if (!context.IsSuccess)
                return Result.FromError(context);
            _contextInjectionService.Context = context.Entity;

            if (gatewayEvent.Data.Value.CustomID.Value is null)
                return Result.FromSuccess();

            ComponentIdFormatter.Parse(gatewayEvent.Data.Value!.CustomID.Value, out ComponentAction action, out string _);

            return action switch
            {
                ComponentAction.WelcomeMessageSetAlternate => await _welcomeMessageService.SetAlternateRoles(gatewayEvent, ct).ConfigureAwait(false),
                ComponentAction.WelcomeMessageNicknameGuess => await _welcomeMessageService.SetNicknameFromGuess(gatewayEvent, ct).ConfigureAwait(false),
                ComponentAction.WelcomeMessageNicknameNoMatch => await _welcomeMessageService.InformNicknameNoMatch(gatewayEvent, ct).ConfigureAwait(false),
                _ => await createInteractionResponse.ConfigureAwait(false),
            };
        }
    }
}
