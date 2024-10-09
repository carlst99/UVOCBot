//
//  InteractionResponder.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneOf;
using Remora.Commands.Services;
using Remora.Commands.Tokenization;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands.Attributes;

namespace UVOCBot.Discord.Core.Responders;

/// <summary>
/// Responds to interactions.
/// </summary>
public class InteractionResponder : IResponder<IInteractionCreate>
{
    private readonly CommandService _commandService;
    private readonly InteractionResponderOptions _options;
    private readonly ExecutionEventCollectorService _eventCollector;
    private readonly IServiceProvider _services;
    private readonly ContextInjectionService _contextInjection;

    private readonly TokenizerOptions _tokenizerOptions;
    private readonly TreeSearchOptions _treeSearchOptions;

    private readonly ITreeNameResolver? _treeNameResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="Remora.Discord.Commands.Responders.InteractionResponder"/> class.
    /// </summary>
    /// <param name="commandService">The command service.</param>
    /// <param name="options">The options.</param>
    /// <param name="eventCollector">The event collector.</param>
    /// <param name="services">The available services.</param>
    /// <param name="contextInjection">The context injection service.</param>
    /// <param name="tokenizerOptions">The tokenizer options.</param>
    /// <param name="treeSearchOptions">The tree search options.</param>
    /// <param name="treeNameResolver">The tree name resolver, if available.</param>
    public InteractionResponder
    (
        CommandService commandService,
        IOptions<InteractionResponderOptions> options,
        ExecutionEventCollectorService eventCollector,
        IServiceProvider services,
        ContextInjectionService contextInjection,
        IOptions<TokenizerOptions> tokenizerOptions,
        IOptions<TreeSearchOptions> treeSearchOptions,
        ITreeNameResolver? treeNameResolver = null
    )
    {
        _commandService = commandService;
        _options = options.Value;
        _eventCollector = eventCollector;
        _services = services;
        _contextInjection = contextInjection;

        _tokenizerOptions = tokenizerOptions.Value;
        _treeSearchOptions = treeSearchOptions.Value;

        _treeNameResolver = treeNameResolver;
    }

    /// <inheritdoc />
    public virtual async Task<Result> RespondAsync
    (
        IInteractionCreate gatewayEvent,
        CancellationToken ct = default
    )
    {
        if (gatewayEvent.Type != InteractionType.ApplicationCommand)
            return Result.FromSuccess();

        if (!gatewayEvent.Data.TryGet(out OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData> data))
            return Result.FromSuccess();

        if (!data.TryPickT0(out IApplicationCommandData? commandData, out _))
            return Result.FromSuccess();

        // Provide the created context to any services inside this scope
        InteractionContext operationContext = new(gatewayEvent);
        _contextInjection.Context = operationContext;

        commandData.UnpackInteraction
        (
            out IReadOnlyList<string>? commandPath,
            out IReadOnlyDictionary<string, IReadOnlyList<string>>? parameters
        );

        if (_treeNameResolver is null)
            return await TryExecuteCommandAsync(operationContext, commandPath, parameters, null, ct);

        Result<string> getTreeName = await _treeNameResolver.GetTreeNameAsync(operationContext, ct);
        if (!getTreeName.IsSuccess)
            return (Result)getTreeName;

        string treeName = getTreeName.Entity;

        return await TryExecuteCommandAsync(operationContext, commandPath, parameters, treeName, ct);
    }

    private async Task<Result> TryExecuteCommandAsync
    (
        InteractionContext operationContext,
        IReadOnlyList<string> commandPath,
        IReadOnlyDictionary<string, IReadOnlyList<string>> parameters,
        string? treeName,
        CancellationToken ct = default
    )
    {
        Result<PreparedCommand> prepareCommand = await _commandService.TryPrepareCommandAsync
        (
            commandPath,
            parameters,
            _services,
            searchOptions: _treeSearchOptions,
            tokenizerOptions: _tokenizerOptions,
            treeName: treeName,
            ct: ct
        );

        if (!prepareCommand.IsSuccess)
        {
            Result preparationError = await _eventCollector.RunPreparationErrorEvents
            (
                _services,
                operationContext,
                prepareCommand,
                ct
            );

            if (!preparationError.IsSuccess)
            {
                return preparationError;
            }

            if (prepareCommand.Error.IsUserOrEnvironmentError())
            {
                // We've done our part and notified whoever might be interested; job well done
                return Result.FromSuccess();
            }

            return (Result)prepareCommand;
        }

        PreparedCommand preparedCommand = prepareCommand.Entity;

        // Update the available context
        InteractionCommandContext commandContext = new(operationContext.Interaction, preparedCommand)
        {
            HasRespondedToInteraction = operationContext.HasRespondedToInteraction
        };
        _contextInjection.Context = commandContext;

        SuppressInteractionResponseAttribute? suppressResponseAttribute = preparedCommand.Command.Node
            .FindCustomAttributeOnLocalTree<SuppressInteractionResponseAttribute>();

        DeferredAttribute? deferredResponseAttribute = preparedCommand.Command.Node
            .FindCustomAttributeOnLocalTree<DeferredAttribute>();

        bool shouldSendResponse = !(suppressResponseAttribute?.Suppress ?? _options.SuppressAutomaticResponses);
        if (shouldSendResponse || (deferredResponseAttribute?.IsDeferred ?? false))
        {
            EphemeralAttribute? ephemeralAttribute = preparedCommand.Command.Node
                .FindCustomAttributeOnLocalTree<EphemeralAttribute>();

            bool sendEphemeral = (ephemeralAttribute is null && _options.UseEphemeralResponses) ||
                ephemeralAttribute?.IsEphemeral == true;

            IInteractionResponseService interactionResponseService = _services.GetRequiredService<IInteractionResponseService>();
            interactionResponseService.WillDefaultToEphemeral = sendEphemeral;

            Result interactionResponse = await interactionResponseService.CreateDeferredMessageResponse(ct);
            if (!interactionResponse.IsSuccess)
            {
                return interactionResponse;
            }

            operationContext.HasRespondedToInteraction = true;
            commandContext.HasRespondedToInteraction = true;
        }

        // Run any user-provided pre-execution events
        Result preExecution = await _eventCollector.RunPreExecutionEvents(_services, commandContext, ct);
        if (!preExecution.IsSuccess)
        {
            return preExecution;
        }

        Result<IResult> executionResult = await _commandService.TryExecuteAsync(preparedCommand, _services, ct);

        // Run any user-provided post-execution events
        return await _eventCollector.RunPostExecutionEvents
        (
            _services,
            commandContext,
            executionResult.IsSuccess ? executionResult.Entity : executionResult,
            ct
        );
    }
}
