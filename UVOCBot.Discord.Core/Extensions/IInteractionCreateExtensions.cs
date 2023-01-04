﻿using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

// ReSharper disable once CheckNamespace
namespace Remora.Discord.API.Abstractions.Gateway.Events;

public static class IInteractionCreateExtensions
{
    /// <summary>
    /// Converts an <see cref="IInteractionCreate"/> object to an <see cref="InteractionContext"/>,
    /// ensuring that the event was generated by a guild member.
    /// </summary>
    /// <param name="gatewayEvent">The gateway event.</param>
    /// <returns></returns>
    public static Result<InteractionContext> ToInteractionContext(this IInteractionCreate gatewayEvent)
        => GetUser(gatewayEvent) is null
            ? new InvalidOperationError("The interaction was not made by a user.")
            : new InteractionContext(gatewayEvent);

    public static IUser? GetUser(this IInteractionCreate gatewayEvent)
        => gatewayEvent.User.HasValue
            ? gatewayEvent.User.Value
            : gatewayEvent.Member.HasValue
                ? gatewayEvent.Member.Value.User.HasValue
                    ? gatewayEvent.Member.Value.User.Value
                    : null
                : null;
}
