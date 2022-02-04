using Remora.Results;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;

namespace UVOCBot.Discord.Core.Errors;

/// <summary>
/// Represents an error caused by an action being performed in the wrong context.
/// </summary>
/// <param name="RequiredContext">The context that the action requires.</param>
public record ContextError
(
    ChannelContext RequiredContext
) : ResultError(string.Empty)
{
    public override string ToString()
        => $"This command must be executed in a { Formatter.InlineQuote(RequiredContext.ToString()) }.";
}
