using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    /// <summary>
    /// Performs setup actions once a connection has been established to the Discord gateway
    /// </summary>
    public class ReadyResponder : IResponder<IReady>
    {
        private readonly ILogger<ReadyResponder> _logger;
        private readonly GeneralOptions _options;
        private readonly DiscordGatewayClient _client;
        private readonly IDbApiService _dbApi;
        private readonly IHostApplicationLifetime _appLifetime;

        public ReadyResponder(
            ILogger<ReadyResponder> logger,
            IOptions<GeneralOptions> options,
            DiscordGatewayClient client,
            IDbApiService dbApi,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;
            _dbApi = dbApi;
            _appLifetime = appLifetime;
        }

        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
        {
            BotConstants.UserId = gatewayEvent.User.ID;

            if (gatewayEvent.Application.ID.HasValue)
                BotConstants.ApplicationId = gatewayEvent.Application.ID.Value;

            _client.SubmitCommandAsync(
                new UpdatePresence(
                    ClientStatus.Online,
                    false,
                    null,
                    Activities: new Activity[] { new Activity(_options.DiscordPresence, ActivityType.Game) }
                )
            );

            await PrepareDatabase(gatewayEvent.Guilds, ct).ConfigureAwait(false);

            _logger.LogInformation("Ready!");
            return Result.FromSuccess();
        }

        private async Task PrepareDatabase(IReadOnlyList<IUnavailableGuild> guilds, CancellationToken ct = default)
        {
            Result dbScaffoldResult = await _dbApi.ScaffoldDbEntries(guilds.Select(g => g.GuildID.Value), ct).ConfigureAwait(false);
            if (!dbScaffoldResult.IsSuccess)
                _appLifetime.StopApplication();
        }
    }
}
