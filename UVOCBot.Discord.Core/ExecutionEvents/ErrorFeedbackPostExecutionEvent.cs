using Microsoft.Extensions.Logging;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Discord.Core.ExecutionEvents;

/// <summary>
/// Handles user-generated errors that cause a command to fail.
/// </summary>
public class ErrorFeedbackPostExecutionEvent : IPostExecutionEvent
{
    private readonly ILogger<ErrorFeedbackPostExecutionEvent> _logger;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly FeedbackService _feedbackService;

    public ErrorFeedbackPostExecutionEvent
    (
        ILogger<ErrorFeedbackPostExecutionEvent> logger,
        IDiscordRestInteractionAPI interactionApi,
        FeedbackService feedbackService
    )
    {
        _logger = logger;
        _interactionApi = interactionApi;
        _feedbackService = feedbackService;
    }

    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = default)
    {
        if (commandResult.IsSuccess)
            return Result.FromSuccess();

        IResultError? actualError = commandResult.Error;

        if (actualError is ConditionNotSatisfiedError)
        {
            IResultError? nestedError = GetFirstInnerErrorOfNotTypeT<ConditionNotSatisfiedError>(commandResult);
            if (nestedError is not null)
                actualError = nestedError;

            // Conditions are checked before the interaction is created.
            if (context is InteractionContext ictx)
            {
                // We're not worrying about an error. It's a rare occurence and more important to log the execution error.
                await _interactionApi.CreateInteractionResponseAsync
                (
                    ictx.ID,
                    ictx.Token,
                    new InteractionResponse
                    (
                        InteractionCallbackType.DeferredChannelMessageWithSource,
                        new InteractionCallbackData(Flags: InteractionCallbackDataFlags.Ephemeral)
                    ),
                    default,
                    ct
                ).ConfigureAwait(false);
            }
        }

        if (actualError is null)
            return Result.FromSuccess();

        string LogUnknownError()
        {
            _logger.LogError("A command failed to execute: {error}", actualError!.ToString());
            return DiscordConstants.GENERIC_ERROR_MESSAGE;
        }

        string errorMessage = actualError switch
        {
            PermissionError pe => pe.ContextualToString(context),
            CommandNotFoundError => "That command doesn't exist.",
            ContextError ce => ce.ToString(),
            RoleManipulationError rme => "Failed to modify roles: " + rme.Message,
            GenericCommandError or ConditionNotSatisfiedError => actualError.Message,
            _ => LogUnknownError()
        };

        IResult sendErrorMessageResult = await _feedbackService.SendContextualErrorAsync(errorMessage, ct: ct).ConfigureAwait(false);

        return !sendErrorMessageResult.IsSuccess
            ? Result.FromError(sendErrorMessageResult.Error!)
            : Result.FromSuccess();
    }

    private static IResultError? GetFirstInnerErrorOfNotTypeT<T>(IResult result)
    {
        IResult? innerResult = result;

        while (innerResult is not null)
        {
            if (innerResult.Error?.GetType() != typeof(T))
                return innerResult.Error;

            innerResult = innerResult.Inner;
        }

        return null;
    }
}
