using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    public class ExecutionEventService : IExecutionEventService
    {
        private readonly ILogger<ExecutionEventService> _logger;
        private readonly IDiscordRestChannelAPI _channelApi;

        public ExecutionEventService(ILogger<ExecutionEventService> logger, IDiscordRestChannelAPI channelApi)
        {
            _logger = logger;
            _channelApi = channelApi;
        }

        public async Task<Result> BeforeExecutionAsync(ICommandContext context, CancellationToken ct = default)
        {
            if (context is MessageContext)
            {
                Result res = await _channelApi.TriggerTypingIndicatorAsync(context.ChannelID, ct).ConfigureAwait(false);
                if (!res.IsSuccess)
                    _logger.LogWarning("Failed to show typing indicator: {message}", res.Error);
            }

            // Always return a successful value. We're not too worried about a failure to show the typing indicator, although we do log it
            return Result.FromSuccess();
        }

        public Task<Result> AfterExecutionAsync(ICommandContext context, IResult executionResult, CancellationToken ct = default)
        {
            return Task.FromResult(Result.FromSuccess());
        }
    }
}
