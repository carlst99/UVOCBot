using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Discord.Core.ExecutionEvents
{
    /// <summary>
    /// Logs unhandled errors that cause a command to fail.
    /// </summary>
    public class ErrorLogPostExecutionEvent : IPostExecutionEvent
    {
        /// <summary>
        /// Gets a list of <see cref="ResultError"/> types that are handled elsewhere (and hence shouldn't be logged here).
        /// </summary>
        private static readonly Type[] HandledErrorTypes = new Type[]
        {
            typeof(PermissionError)
        };

        private readonly ILogger<ErrorLogPostExecutionEvent> _logger;

        public ErrorLogPostExecutionEvent(ILogger<ErrorLogPostExecutionEvent> logger)
        {
            _logger = logger;
        }

        public Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = default)
        {
            if (!commandResult.IsSuccess && commandResult.Error is not null && !HandledErrorTypes.Contains(commandResult.Error.GetType()))
                _logger.LogError("A command failed to execute: {error}", commandResult.Error.ToString());

            return Task.FromResult(Result.FromSuccess());
        }
    }
}
