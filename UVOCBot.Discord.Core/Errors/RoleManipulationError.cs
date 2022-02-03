using Remora.Results;

namespace UVOCBot.Discord.Core.Errors;

public record RoleManipulationError : ResultError
{
    public RoleManipulationError(string Message)
        : base(Message)
    {
    }

    public RoleManipulationError(ResultError original)
        : base(original)
    {
    }
}
