using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
using UVOCBot.Discord.Core.Components;

namespace UVOCBot.Discord.Core.Responders;

internal sealed class ComponentInteractionResponder : IResponder<IInteractionCreate>
{
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly ComponentResponderRepository _componentRepository;
    private readonly IServiceProvider _services;
    private readonly ContextInjectionService _contextInjectionService;

    public ComponentInteractionResponder
    (
        IDiscordRestInteractionAPI interactionApi,
        IOptions<ComponentResponderRepository> componentRepository,
        IServiceProvider services,
        ContextInjectionService contextInjectionService
    )
    {
        _interactionApi = interactionApi;
        _componentRepository = componentRepository.Value;
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

        InteractionResponse response = new
        (
            InteractionCallbackType.DeferredChannelMessageWithSource,
            new InteractionCallbackData(Flags: InteractionCallbackDataFlags.Ephemeral)
        );

        Result createInteractionResponse = await _interactionApi.CreateInteractionResponseAsync
        (
            gatewayEvent.ID,
            gatewayEvent.Token,
            response,
            default,
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

        ComponentIdFormatter.Parse
        (
            gatewayEvent.Data.Value!.CustomID.Value,
            out string key,
            out string payload
        );

        // Naively run sequentially, this could be improved
        foreach (Type responderType in _componentRepository.GetResponders(key))
        {
            IComponentResponder responder = (IComponentResponder)_services.GetRequiredService(responderType);
            Result responderResult = await responder.RespondAsync(key, payload, ct).ConfigureAwait(false);

            if (!responderResult.IsSuccess)
                return responderResult;
        }

        return Result.FromSuccess();
    }
}
