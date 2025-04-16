using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
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
    private readonly Dictionary<string, IReadOnlyList<Attribute[]>> _componentKeyResponseAttributes;

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
        _componentKeyResponseAttributes = new Dictionary<string, IReadOnlyList<Attribute[]>>();
    }

    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
    {
        if (gatewayEvent.Type is not (InteractionType.MessageComponent or InteractionType.ModalSubmit))
            return Result.FromSuccess();

        if (!gatewayEvent.Data.IsDefined(out OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData> data))
            return Result.FromSuccess();

        if (data.IsT0)
            return Result.FromSuccess();

        string customID = data.IsT1
            ? data.AsT1.CustomID
            : data.AsT2.CustomID;

        // Get the user who initiated the interaction
        IUser? user = gatewayEvent.GetUser();
        if (user is null)
            return Result.FromSuccess();

        // Provide the created context to any services inside this scope
        Result<InteractionContext> getContext = gatewayEvent.ToInteractionContext();
        if (!getContext.IsDefined(out InteractionContext? operationContext))
            return Result.FromError(getContext);
        _contextInjectionService.Context = operationContext;

        // Update the available context
        InteractionCommandContext commandContext = new(operationContext.Interaction, null!)
        {
            HasRespondedToInteraction = operationContext.HasRespondedToInteraction
        };
        _contextInjectionService.Context = commandContext;

        // Run any user-provided pre-execution events
        Result preExecution = await _eventCollector.RunPreExecutionEvents(_services, commandContext, ct);
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
        List<IComponentResponder> responders = responderList.Select
            (
                responderType => (IComponentResponder)_services.GetRequiredService(responderType)
            )
            .ToList();

        IReadOnlyList<Attribute[]> componentAttributes = GetKeyAttributes(key, responders);
        if (componentAttributes.SelectMany(x => x).Any(x => x.GetType() == typeof(EphemeralAttribute)))
            interactionResponseService.WillDefaultToEphemeral = true;

        if (!_interactionResponderOptions.SuppressAutomaticResponses)
        {
            Result createInteractionResponse = await interactionResponseService.CreateDeferredMessageResponse(ct);
            if (!createInteractionResponse.IsSuccess)
                return createInteractionResponse;
        }

        // Naively run sequentially, this could be improved
        foreach (IComponentResponder responder in responders)
        {
            IResult responderResult = await responder.RespondAsync(key, payload, ct).ConfigureAwait(false);

            await _eventCollector.RunPostExecutionEvents
            (
                _services,
                commandContext,
                responderResult,
                ct
            ).ConfigureAwait(false);
        }

        return Result.FromSuccess();
    }

    private IReadOnlyList<Attribute[]> GetKeyAttributes(string key, IEnumerable<IComponentResponder> participatingResponders)
    {
        if (_componentKeyResponseAttributes.TryGetValue(key, out IReadOnlyList<Attribute[]>? retrievedAttributeList))
            return retrievedAttributeList;

        List<Attribute[]> builtAttributeList = new();
        foreach (IComponentResponder resp in participatingResponders)
        {
            if (!resp.GetResponseAttributes(key).IsDefined(out Attribute[]? attributes))
                continue;

            builtAttributeList.Add(attributes);
        }
        _componentKeyResponseAttributes.Add(key, builtAttributeList);

        return builtAttributeList;
    }
}
