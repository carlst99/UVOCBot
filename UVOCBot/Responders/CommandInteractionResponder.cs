using Remora.Commands.Services;
using Remora.Commands.Trees;
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
using System.Threading;
using System.Threading.Tasks;
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

            IApplicationCommandInteractionData interactionData = gatewayEvent.Data.Value!;
            interactionData.UnpackInteraction(out var command, out var parameters);

            // Run any user-provided pre execution events
            Result preExecutionResult = await _eventCollector.RunPreExecutionEvents(_services, context.Entity, ct).ConfigureAwait(false);
            if (!preExecutionResult.IsSuccess)
                return preExecutionResult;

            // Run the actual command
            var searchOptions = new TreeSearchOptions(StringComparison.OrdinalIgnoreCase);
            var executeResult = await _commandService.TryExecuteAsync
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
    }
}
