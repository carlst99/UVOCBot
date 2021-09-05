using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Commands.ExecutionEvents
{
    public class ErrorLogExecutionEvent : IPostExecutionEvent
    {
        private readonly ILogger<ErrorLogExecutionEvent> _logger;

        public ErrorLogExecutionEvent(ILogger<ErrorLogExecutionEvent> logger)
        {
            _logger = logger;
        }

        public Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = default)
        {
            if (!commandResult.IsSuccess)
                _logger.LogError("A command failed to complete: {error}", commandResult.Error);

            return Task.FromResult(Result.FromSuccess());
        }
    }
}
