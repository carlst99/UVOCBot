using Microsoft.Extensions.Logging;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using UVOCBot.Discord.Core.Commands;
using Remora.Discord.Commands.Services;
using Remora.Rest.Results;
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
    private readonly FeedbackService _feedbackService;

    public ErrorFeedbackPostExecutionEvent
    (
        ILogger<ErrorFeedbackPostExecutionEvent> logger,
        FeedbackService feedbackService
    )
    {
        _logger = logger;
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
        }

        if (actualError is null)
            return Result.FromSuccess();

        string LogUnknownError()
        {
            _logger.LogError("A command failed to execute: {Error}", actualError);
            return DiscordConstants.GENERIC_ERROR_MESSAGE;
        }

        string errorMessage = actualError switch
        {
            PermissionError pe => pe.ContextualToString(context),
            CommandNotFoundError => "That command doesn't exist.",
            ContextError ce => ce.ToString(),
            ParameterParsingError ppe => $"You've entered an invalid value for the {Formatter.InlineQuote(ppe.Parameter.ParameterShape.HintName)} parameter",
            RoleManipulationError rme => "Failed to modify roles: " + rme.Message,
            GenericCommandError or ConditionNotSatisfiedError or InvalidOperationError => actualError.Message,
            RestResultError<RestError> rre when rre.Error.Code is DiscordError.MissingAccess => "I am not allowed to view this channel.",
            RestResultError<RestError> rre when rre.Error.Code is DiscordError.MissingPermission => "I do not have permission to do that.",
            RestResultError<RestError> rre when rre.Error.Code is DiscordError.OwnerOnly => "Only the owner can do that!",
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
