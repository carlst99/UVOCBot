using Remora.Results;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;

namespace UVOCBot.Discord.Core.Errors;

public record ContextError : ResultError
{
    /// <summary>
    /// The required context.
    /// </summary>
    public ChannelContext RequiredContext { get; init; }

    public ContextError(ChannelContext requiredContext)
        : this(requiredContext, string.Empty)
    {
    }

    public ContextError(ChannelContext requiredContext, string Message)
        : base(Message)
    {
        RequiredContext = requiredContext;
    }

    public ContextError(ChannelContext requiredContext, ResultError original)
        : base(original)
    {
        RequiredContext = requiredContext;
    }
}
