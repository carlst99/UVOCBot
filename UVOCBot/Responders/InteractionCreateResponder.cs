using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class InteractionCreateResponder : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IWelcomeMessageService _welcomeMessageService;

        public InteractionCreateResponder(IDiscordRestInteractionAPI interactionApi, IWelcomeMessageService welcomeMessageService)
        {
            _interactionApi = interactionApi;
            _welcomeMessageService = welcomeMessageService;
        }

        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.Data.Value is null)
                return Result.FromSuccess();

            // Get the user who initiated the interaction
            IUser? user = gatewayEvent.User.HasValue
                ? gatewayEvent.User.Value
                : gatewayEvent.Member.HasValue
                    ? gatewayEvent.Member.Value.User.HasValue
                        ? gatewayEvent.Member.Value.User.Value
                        : null
                    : null;

            if (user is null)
                return Result.FromSuccess();

            var response = new InteractionResponse(InteractionCallbackType.DeferredChannelMessageWithSource);

            // If it's a message component, force emphemerality.
            if (gatewayEvent.Type == InteractionType.MessageComponent)
                response = response with { Data = new InteractionApplicationCommandCallbackData(Flags: MessageFlags.Ephemeral) };

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

            if (gatewayEvent.Type == InteractionType.MessageComponent)
            {
                if (gatewayEvent.Data.Value.CustomID.Value is null)
                    return Result.FromSuccess();

                ComponentIdFormatter.Parse(gatewayEvent.Data.Value!.CustomID.Value, out ComponentAction action, out string _);

                switch (action)
                {
                    case ComponentAction.WelcomeMessageSetAlternate:
                        return await _welcomeMessageService.SetAlternateRoles(gatewayEvent, ct).ConfigureAwait(false);
                    case ComponentAction.WelcomeMessageNicknameGuess:
                        return await _welcomeMessageService.SetNicknameFromGuess(gatewayEvent, ct).ConfigureAwait(false);
                }
            }

            return await createInteractionResponse.ConfigureAwait(false);
        }
    }
}
