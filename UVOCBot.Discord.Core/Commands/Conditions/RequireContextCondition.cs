using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Discord.Core.Commands.Conditions
{
    /// <summary>
    /// Checks required contexts before allowing execution.
    /// </summary>
    public class RequireContextCondition : ICondition<RequireContextAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireContextCondition"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channelAPI">The channel API.</param>
        public RequireContextCondition
        (
            ICommandContext context,
            IDiscordRestChannelAPI channelAPI
        )
        {
            _context = context;
            _channelAPI = channelAPI;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequireContextAttribute attribute, CancellationToken ct = default)
        {
            Result<IChannel> getChannel = await _channelAPI.GetChannelAsync(_context.ChannelID, ct).ConfigureAwait(false);
            if (!getChannel.IsSuccess)
                return Result.FromError(getChannel);

            IChannel channel = getChannel.Entity;

            if (attribute.Context is ChannelContext.DM && channel.Type is not ChannelType.DM)
                return new ContextError(ChannelContext.DM);
            else if (attribute.Context is ChannelContext.GroupDM && channel.Type is not ChannelType.GroupDM)
                return new ContextError(ChannelContext.GroupDM);
            else if (attribute.Context is ChannelContext.Guild && channel.Type is ChannelType.DM or ChannelType.GroupDM)
                return new ContextError(ChannelContext.Guild);
            else
                return Result.FromSuccess();
        }
    }
}
