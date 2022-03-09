using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Components;

namespace UVOCBot.Discord.Core.Responders;

public class ComponentInteractionResponder : IResponder<IInteractionCreate>
{
    private readonly ILogger<ComponentInteractionResponder> _logger;
    private readonly ComponentResponderRepository _componentRepository;
    private readonly InteractionResponderOptions _interactionResponderOptions;
    private readonly IServiceProvider _services;
    private readonly ContextInjectionService _contextInjectionService;
    private readonly ExecutionEventCollectorService _eventCollector;

    public ComponentInteractionResponder
    (
        ILogger<ComponentInteractionResponder> logger,
        IOptions<ComponentResponderRepository> componentRepository,
        IOptions<InteractionResponderOptions> interactionResponderOptions,
        IServiceProvider services,
        ContextInjectionService contextInjectionService,
        ExecutionEventCollectorService eventCollector
    )
    {
        _logger = logger;
        _componentRepository = componentRepository.Value;
        _interactionResponderOptions = interactionResponderOptions.Value;
        _services = services;
        _contextInjectionService = contextInjectionService;
        _eventCollector = eventCollector;
    }

    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
    {
        if (gatewayEvent.Type is not (InteractionType.MessageComponent or InteractionType.ModalSubmit))
            return Result.FromSuccess();

        if (!gatewayEvent.Data.IsDefined(out IInteractionData? interactionData))
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

        if (!interactionData.CustomID.IsDefined(out string? customID))
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
            customID,
            out string key,
            out string? payload
        );

        IReadOnlyList<Type> responderList = _componentRepository.GetResponders(key);
        if (responderList.Count == 0)
        {
            _logger.LogWarning("A component interaction with the key {Key} was received, but no responders have been registered for this key", key);
            return Result.FromSuccess();
        }

        IInteractionResponseService interactionResponseService = _services.GetRequiredService<IInteractionResponseService>();

        if (responderList.Any(r => r.GetCustomAttribute<EphemeralAttribute>() is not null))
            interactionResponseService.WillDefaultToEphemeral = true;

        if (!_interactionResponderOptions.SuppressAutomaticResponses)
        {
            Result createInteractionResponse = await interactionResponseService.CreateDeferredMessageResponse(ct);
            if (!createInteractionResponse.IsSuccess)
                return createInteractionResponse;
        }

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
