using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Results;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;

namespace UVOCBot.Commands.Conditions
{
    /// <summary>
    /// Checks required contexts before allowing execution.
    /// </summary>
    public class RequireContextCondition : ICondition<RequireContextAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly MessageResponseHelpers _responder;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireContextCondition"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="responder">The message responder.</param>
        public RequireContextCondition
        (
            ICommandContext context,
            IDiscordRestChannelAPI channelAPI,
            MessageResponseHelpers responder
        )
        {
            _context = context;
            _channelAPI = channelAPI;
            _responder = responder;
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
                await _responder.RespondWithUserErrorAsync(_context, "This command can only be used in a DM.", ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("This command can only be used in a DM.");
            }
            else if (attribute.Context is ChannelContext.GroupDM && channel.Type is not ChannelType.GroupDM)
            {
                await _responder.RespondWithUserErrorAsync(_context, "This command can only be used in a group DM.", ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("This command can only be used in a group DM.");
            }
            else if (attribute.Context is ChannelContext.GroupDM && channel.Type is ChannelType.DM or ChannelType.GroupDM)
            {
                await _responder.RespondWithUserErrorAsync(_context, "This command can only be used in a guild.", ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("This command can only be used in a guild.");
            }
            else
            {
                return Result.FromSuccess();
            }
        }
    }
}
