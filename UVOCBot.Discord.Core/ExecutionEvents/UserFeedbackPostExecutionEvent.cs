using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Results;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Discord.Core.ExecutionEvents
{
    /// <summary>
    /// Handles user-generated errors that cause a command to fail.
    /// </summary>
    public class UserFeedbackPostExecutionEvent : IPostExecutionEvent
    {
        private readonly FeedbackService _feedbackService;

        public UserFeedbackPostExecutionEvent(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = default)
        {
            if (commandResult.IsSuccess)
                return Result.FromSuccess();

            IResult? sendErrorMessageResult = null;

            if (commandResult.Error is PermissionError permissionError)
            {
                string userMention = permissionError.UserID == context.User.ID ? "You don't" : Formatter.UserMention(permissionError.UserID) + " doesn't";
                string channelMention = permissionError.ChannelID == context.ChannelID ? "this channel" : Formatter.ChannelMention(permissionError.ChannelID);

                sendErrorMessageResult = await _feedbackService.SendContextualErrorAsync(
                    $"{ userMention } have the required { permissionError.Permission } permission in { channelMention }.",
                    ct: ct).ConfigureAwait(false);
            }
            else if (commandResult.Error is GenericCommandError genericError)
            {
                sendErrorMessageResult = await _feedbackService.SendContextualErrorAsync(genericError.Message, ct: ct).ConfigureAwait(false);
            }

            return sendErrorMessageResult?.IsSuccess == false
                ? Result.FromError(sendErrorMessageResult.Error!)
                : Result.FromSuccess();
        }
    }
}
