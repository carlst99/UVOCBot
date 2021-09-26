using Remora.Commands.Conditions;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;

namespace UVOCBot.Discord.Core.Commands.Conditions
{
    /// <summary>
    /// Checks required contexts before allowing execution.
    /// </summary>
    public class RequireContextCondition : ICondition<RequireContextAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly FeedbackService _feedbackService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireContextCondition"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="feedbackService">The message responder.</param>
        public RequireContextCondition
        (
            ICommandContext context,
            IDiscordRestChannelAPI channelAPI,
            FeedbackService feedbackService
        )
        {
            _context = context;
            _channelAPI = channelAPI;
            _feedbackService = feedbackService;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequireContextAttribute attribute, CancellationToken ct = default)
        {
            Result<IChannel> getChannel = await _channelAPI.GetChannelAsync(_context.ChannelID, ct).ConfigureAwait(false);
            if (!getChannel.IsSuccess)
                return Result.FromError(getChannel);

            IChannel channel = getChannel.Entity;

            if (attribute.Context is ChannelContext.DM && channel.Type is not ChannelType.DM)
            {
                await _feedbackService.SendContextualErrorAsync("This command can only be used in a DM.", ct: ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("This command can only be used in a DM.");
            }
            else if (attribute.Context is ChannelContext.GroupDM && channel.Type is not ChannelType.GroupDM)
            {
                await _feedbackService.SendContextualErrorAsync("This command can only be used in a group DM.", ct: ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("This command can only be used in a group DM.");
            }
            else if (attribute.Context is ChannelContext.GroupDM && channel.Type is ChannelType.DM or ChannelType.GroupDM)
            {
                await _feedbackService.SendContextualErrorAsync("This command can only be used in a guild.", ct: ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("This command can only be used in a guild.");
            }
            else
            {
                return Result.FromSuccess();
            }
        }
    }
}
