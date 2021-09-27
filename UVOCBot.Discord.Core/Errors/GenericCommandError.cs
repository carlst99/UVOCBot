using Remora.Results;

namespace UVOCBot.Discord.Core.Errors
{
    public record GenericCommandError : ResultError
    {
        public GenericCommandError()
            : base("Something went wrong! Please try again.")
        {
        }

        public GenericCommandError(string Message)
            : base(Message)
        {
        }

        public GenericCommandError(ResultError original)
            : base(original)
        {
        }
    }
}
