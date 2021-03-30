using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBotRemora.Config;
using UVOCBotRemora.Services;

namespace UVOCBotRemora.Responders
{
    public class ReadyResponder : IResponder<IReady>
    {
        private readonly DiscordGatewayClient _client;
        private readonly GeneralOptions _options;
        private readonly IPrefixService _prefixService;
        private readonly ILogger<ReadyResponder> _logger;

        public ReadyResponder(
            DiscordGatewayClient client,
            IOptions<GeneralOptions> options,
            IPrefixService prefixService,
            ILogger<ReadyResponder> logger)
        {
            _client = client;
            _options = options.Value;
            _prefixService = prefixService;
            _logger = logger;
        }

        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
        {
            await _prefixService.SetupAsync().ConfigureAwait(false);

            _client.SubmitCommandAsync(
                new UpdateStatus(
                    ClientStatus.Online,
                    false,
                    Activities: new Activity[] { new Activity(_options.CommandPrefix + "help", ActivityType.Listening) }
                    )
                );

            _logger.LogInformation("Ready!");
            return Result.FromSuccess();
        }
    }
}
