using Remora.Discord.API.Abstractions.Objects;
using Remora.Results;
using System.Collections.Generic;

namespace UVOCBot.Discord.Core.Errors;

/// <summary>
/// Represents an error caused by an action being performed in the wrong context.
/// </summary>
/// <param name="RequiredContexts">The context/s that the action requires.</param>
public record ContextError
(
    IReadOnlyList<ChannelType> RequiredContexts
) : ResultError(string.Empty)
{
    public static readonly IReadOnlyList<ChannelType> GuildTextChannels = new[] {
        ChannelType.GuildNews,
        ChannelType.GuildText,
        ChannelType.GuildNewsThread,
        ChannelType.GuildPrivateThread,
        ChannelType.GuildPublicThread,
        ChannelType.GuildNewsThread
    };

    public override string ToString()
        => $"This command can only be executed in one of the following channel types: {Formatter.InlineQuote(string.Join(", ", RequiredContexts))}.";
}
