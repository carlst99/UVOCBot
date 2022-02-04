using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Components;

namespace UVOCBot.Discord.Core.Responders;

internal sealed class ComponentInteractionResponder : IResponder<IInteractionCreate>
{
    private readonly ILogger<ComponentInteractionResponder> _logger;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly ComponentResponderRepository _componentRepository;
    private readonly IServiceProvider _services;
    private readonly ContextInjectionService _contextInjectionService;
    private readonly ExecutionEventCollectorService _eventCollector;

    public ComponentInteractionResponder
    (
        ILogger<ComponentInteractionResponder> logger,
        IDiscordRestInteractionAPI interactionApi,
        IOptions<ComponentResponderRepository> componentRepository,
        IServiceProvider services,
        ContextInjectionService contextInjectionService,
        ExecutionEventCollectorService eventCollector
    )
    {
        _logger = logger;
        _interactionApi = interactionApi;
        _componentRepository = componentRepository.Value;
        _services = services;
        _contextInjectionService = contextInjectionService;
        _eventCollector = eventCollector;
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

        // Provide the created context to any services inside this scope
        Result<InteractionContext> context = gatewayEvent.ToInteractionContext();
        if (!context.IsSuccess)
            return Result.FromError(context);
        _contextInjectionService.Context = context.Entity;

        if (gatewayEvent.Data.Value.CustomID.Value is null)
            return Result.FromSuccess();

        // Run any user-provided pre-execution events
        Result preExecution = await _eventCollector.RunPreExecutionEvents
        (
            _services,
            context.Entity,
            ct
        ).ConfigureAwait(false);

        if (!preExecution.IsSuccess)
            return preExecution;

        ComponentIDFormatter.Parse
        (
            gatewayEvent.Data.Value!.CustomID.Value,
            out string key,
            out string? payload
        );

        IReadOnlyList<Type> responderList = _componentRepository.GetResponders(key);
        if (responderList.Count == 0)
        {
            _logger.LogWarning("A component interaction with the key {key} was received, but no responders have been registered for this key.", key);
            return Result.FromSuccess();
        }

        InteractionCallbackDataFlags flags = InteractionCallbackDataFlags.Ephemeral;
        if (responderList.Count == 1 && responderList[0].GetCustomAttribute<EphemeralAttribute>() is null)
                flags &= ~InteractionCallbackDataFlags.Ephemeral;

        InteractionResponse response = new
        (
            InteractionCallbackType.DeferredChannelMessageWithSource,
            new InteractionCallbackData(Flags: flags)
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

        // Naively run sequentially, this could be improved
        foreach (Type responderType in responderList)
        {
            IComponentResponder responder = (IComponentResponder)_services.GetRequiredService(responderType);
            IResult responderResult = await responder.RespondAsync(key, payload, ct).ConfigureAwait(false);

            await _eventCollector.RunPostExecutionEvents
            (
                _services,
                context.Entity,
                responderResult,
                ct
            ).ConfigureAwait(false);
        }

        return Result.FromSuccess();
    }
}
