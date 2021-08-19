﻿using Remora.Commands.Results;
using Remora.Commands.Services;
using Remora.Commands.Signatures;
using Remora.Commands.Trees;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands.Utilities;
using UVOCBot.Extensions;

namespace UVOCBot.Responders
{
    public class CommandInteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly ContextInjectionService _contextInjectionService;
        private readonly CommandService _commandService;
        private readonly ExecutionEventCollectorService _eventCollector;
        private readonly IServiceProvider _services;

        public CommandInteractionResponder(
            IDiscordRestInteractionAPI interactionApi,
            ContextInjectionService contextInjectionService,
            CommandService commandService,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services)
        {
            _interactionApi = interactionApi;
            _contextInjectionService = contextInjectionService;
            _commandService = commandService;
            _eventCollector = eventCollector;
            _services = services;
        }

        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.Type != InteractionType.ApplicationCommand)
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

            var response = new InteractionResponse(InteractionCallbackType.DeferredChannelMessageWithSource);

            // Provide the created context to any services inside this scope
            Result<InteractionContext> context = gatewayEvent.ToInteractionContext();
            if (!context.IsSuccess)
                return Result.FromError(context);
            _contextInjectionService.Context = context.Entity;

            IInteractionData interactionData = gatewayEvent.Data.Value!;
            interactionData.UnpackInteraction(out var command, out var parameters);

            Result<BoundCommandNode> getCommandResult = GetCommandNode(command, parameters);
            if (!getCommandResult.IsSuccess)
                return Result.FromError(getCommandResult);

            if (IsEphemeral(getCommandResult.Entity))
                response = response with { Data = new InteractionCallbackData(Flags: InteractionCallbackDataFlags.Ephemeral) };

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

            // Run any user-provided pre execution events
            Result preExecutionResult = await _eventCollector.RunPreExecutionEvents(_services, context.Entity, ct).ConfigureAwait(false);
            if (!preExecutionResult.IsSuccess)
                return preExecutionResult;

            // Run the actual command
            TreeSearchOptions searchOptions = new(StringComparison.OrdinalIgnoreCase);
            Result<IResult> executeResult = await _commandService.TryExecuteAsync
            (
                command,
                parameters,
                _services,
                searchOptions: searchOptions,
                ct: ct
            ).ConfigureAwait(false);

            if (!executeResult.IsSuccess)
                return Result.FromError(executeResult);

            // Run any user-provided post execution events
            Result postExecutionResult = await _eventCollector.RunPostExecutionEvents(_services, context.Entity, executeResult.Entity, ct).ConfigureAwait(false);
            if (!postExecutionResult.IsSuccess)
                return postExecutionResult;

            return await createInteractionResponse.ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to find a command in the command tree.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="commandParameters">The parameters of the command.</param>
        /// <returns>A <see cref="Result{BoundCommandNode}"/> indicating if the command was successfully found, and containing the command node if so.</returns>
        private Result<BoundCommandNode> GetCommandNode(string commandName, IReadOnlyDictionary<string, IReadOnlyList<string>> commandParameters)
        {
            TreeSearchOptions searchOptions = new(StringComparison.OrdinalIgnoreCase);
            List<BoundCommandNode> commands = _commandService.Tree.Search(commandName, commandParameters, searchOptions: searchOptions).ToList();

            if (commands.Count == 0)
                return new CommandNotFoundError(commandName);

            if (commands.Count > 1)
                return new AmbiguousCommandInvocationError();

            return commands.Single();
        }

        /// <summary>
        /// Gets a value indicating that the given command should produce an ephemeral response.
        /// </summary>
        /// <param name="commandNode">The command to check.</param>
        /// <returns>A value indicating if an ephemeral response should be created.</returns>
        private static bool IsEphemeral(BoundCommandNode commandNode)
        {
            // Attempt to first check for ephemerailty on the command itself
            EphemeralAttribute? attr = commandNode.Node.CommandMethod.GetCustomAttribute<EphemeralAttribute>();

            if (attr is null)
            {
                // Traverse each parent group node, until we find the root node
                IParentNode p = commandNode.Node.Parent;
                while (p is GroupNode g && attr is null)
                {
                    p = g.Parent;

                    // See if any of the types in this group have expressed ephemerality
                    foreach (Type t in g.GroupTypes)
                    {
                        attr = t.GetCustomAttribute<EphemeralAttribute>();
                        if (attr is not null)
                            break;
                    }
                }
            }

            return attr?.IsEphemeral == true;
        }
    }
}
