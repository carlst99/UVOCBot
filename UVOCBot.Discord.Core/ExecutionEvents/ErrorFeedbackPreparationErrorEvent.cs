using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Discord.Core.ExecutionEvents;

public class ErrorFeedbackPreparationErrorEvent : IPreparationErrorEvent
{
    private readonly ILogger<ErrorFeedbackPreparationErrorEvent> _logger;
    private readonly IFeedbackService _feedbackService;

    public ErrorFeedbackPreparationErrorEvent
    (
        ILogger<ErrorFeedbackPreparationErrorEvent> logger,
        IFeedbackService feedbackService
    )
    {
        _logger = logger;
        _feedbackService = feedbackService;
    }

    public async Task<Result> PreparationFailed
    (
        IOperationContext context,
        IResult preparationResult,
        CancellationToken ct = default
    )
    {
        IResultError? prepError = preparationResult.Error;
        if (prepError is null)
            return Result.FromSuccess();

        _logger.LogDebug("A command failed to prepare due to a user / environmental issue: {Error}", prepError);

        Result<IReadOnlyList<IMessage>> sendErrorMessageResult
            = await _feedbackService.SendContextualErrorAsync(prepError.Message, ct: ct);

        return !sendErrorMessageResult.IsSuccess
            ? Result.FromError(sendErrorMessageResult.Error!)
            : Result.FromSuccess();
    }
}
