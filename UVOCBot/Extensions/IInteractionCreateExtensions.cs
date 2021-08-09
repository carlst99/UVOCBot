using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace UVOCBot.Extensions
{
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
                return Result<InteractionContext>.FromError(Result.FromSuccess()); // Lazy man's way of getting around no generic error class and having to work with this silly Results infrastructure

            IApplicationCommandInteractionData interactionData = gatewayEvent.Data.Value!;
            InteractionContext context = new
            (
                gatewayEvent.GuildID,
                gatewayEvent.ChannelID.Value,
                user,
                gatewayEvent.Member,
                gatewayEvent.Token,
                gatewayEvent.ID,
                gatewayEvent.ApplicationID,
                interactionData
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
}
