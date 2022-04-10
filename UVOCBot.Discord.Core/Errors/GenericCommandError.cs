using Remora.Results;

namespace UVOCBot.Discord.Core.Errors;

public record GenericCommandError : ResultError
{
    public GenericCommandError()
        : base(DiscordConstants.GENERIC_ERROR_MESSAGE)
    {
    }

    public GenericCommandError(string Message)
        : base(Message)
    {
    }
}
