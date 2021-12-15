using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Remora.Discord.API.Abstractions.Gateway.Events;

public static class IInteractionCreateExtensions
{
    public static Result<InteractionContext> ToInteractionContext(this IInteractionCreate gatewayEvent)
    {
        IUser? user = gatewayEvent.User.HasValue
            ? gatewayEvent.User.Value
            : gatewayEvent.Member.HasValue
                ? gatewayEvent.Member.Value.User.HasValue
                    ? gatewayEvent.Member.Value.User.Value
                    : null
                : null;

        if (user is null)
            return Result<InteractionContext>.FromError(new InvalidOperationError("The interaction was not made by a user."));

        InteractionContext context = new
        (
            gatewayEvent.GuildID,
            gatewayEvent.ChannelID.Value,
            user,
            gatewayEvent.Member,
            gatewayEvent.Token,
            gatewayEvent.ID,
            gatewayEvent.ApplicationID,
            gatewayEvent.Data.Value!, // We can assume this is non-null for the time being
            gatewayEvent.Message
        );

        return Result<InteractionContext>.FromSuccess(context);
    }

    public static IUser? GetUser(this IInteractionCreate gatewayEvent)
        => gatewayEvent.User.HasValue
            ? gatewayEvent.User.Value
            : gatewayEvent.Member.HasValue
                ? gatewayEvent.Member.Value.User.HasValue
                    ? gatewayEvent.Member.Value.User.Value
                    : null
                : null;
}
