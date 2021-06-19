using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class ComponentInteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IWelcomeMessageService _welcomeMessageService;

        public ComponentInteractionResponder(
            IDiscordRestInteractionAPI interactionApi,
            IWelcomeMessageService welcomeMessageService)
        {
            _interactionApi = interactionApi;
            _welcomeMessageService = welcomeMessageService;
        }

        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.Type != InteractionType.MessageComponent)
                return Result.FromSuccess();

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
