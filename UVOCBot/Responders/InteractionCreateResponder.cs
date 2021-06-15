﻿using Remora.Discord.API.Abstractions.Gateway.Events;
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
            // Signal to Discord that we'll be handling this one asynchronously, with an ephemeral response
            var response = new InteractionResponse
            (
                InteractionCallbackType.DeferredChannelMessageWithSource,
                new InteractionApplicationCommandCallbackData(Flags: MessageFlags.Ephemeral
            ));

            Result interactionResponse = await _interactionApi.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                response,
                ct
            ).ConfigureAwait(false);

            if (!interactionResponse.IsSuccess)
                return interactionResponse;

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

            if (gatewayEvent.Type == InteractionType.MessageComponent)
            {
                if (gatewayEvent.Data.Value is null || gatewayEvent.Data.Value.CustomID.Value is null)
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

            return Result.FromSuccess();
        }
    }
}
