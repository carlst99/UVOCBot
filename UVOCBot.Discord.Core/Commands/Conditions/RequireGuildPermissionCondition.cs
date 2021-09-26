using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Discord.Core.Commands.Conditions
{
    /// <summary>
    /// Checks required Guild permissions before allowing execution.
    /// <remarks>Fails if the command is executed outside of a Guild./></remarks>
    /// </summary>
    public class RequireGuildPermissionCondition : ICondition<RequireGuildPermissionAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly FeedbackService _feedbackService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireGuildPermissionCondition"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="feedbackService">The message responder.</param>
        public RequireGuildPermissionCondition(
            ICommandContext context,
            IPermissionChecksService permissionChecksService,
            FeedbackService feedbackService)
        {
            _context = context;
            _permissionChecksService = permissionChecksService;
            _feedbackService = feedbackService;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequireGuildPermissionAttribute attribute, CancellationToken ct = default)
        {
            if (!_context.GuildID.HasValue)
            {
                IResult sendErrorResult = await _feedbackService.SendContextualErrorAsync(
                    "This command must be executed within a guild.",
                    ct: ct).ConfigureAwait(false);

                return sendErrorResult.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(sendErrorResult.Error!);
            }

            Result<IDiscordPermissionSet> getCallerPermissions = await _permissionChecksService.GetPermissionsInChannel(
                _context.ChannelID,
                _context.User.ID,
                ct).ConfigureAwait(false);

            Result<IDiscordPermissionSet> getBotPermissions = await _permissionChecksService.GetPermissionsInChannel(
                _context.ChannelID,
                DiscordConstants.UserId,
                ct).ConfigureAwait(false);

            if (!getCallerPermissions.IsDefined() || !getBotPermissions.IsDefined())
            {
                await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: ct).ConfigureAwait(false);
                return Result.FromError(getCallerPermissions);
            }

            if (attribute.IncludeCurrent && !getBotPermissions.Entity.HasAdminOrPermission(attribute.Permission))
            {
                await _feedbackService.SendContextualErrorAsync(
                    $"<@{ DiscordConstants.UserId }> (that's me!) needs the { Formatter.InlineQuote(attribute.Permission.ToString()) } permission in this channel to perform this action.",
                    ct: ct).ConfigureAwait(false);
            }

            if (!getCallerPermissions.Entity.HasAdminOrPermission(attribute.Permission))
            {
                await _feedbackService.SendContextualErrorAsync(
                        $"You need the { Formatter.InlineQuote(attribute.Permission.ToString()) } permission in this channel to use this command.",
                        ct: ct).ConfigureAwait(false);
            }

            return Result.FromSuccess();
        }
    }
}
