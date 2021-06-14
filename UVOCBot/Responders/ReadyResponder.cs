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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Model;
using UVOCBot.Model;
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
            foreach (IUnavailableGuild guild in guilds)
            {
                Result<GuildSettingsDTO> guildSettingsResult = await _dbApi.CreateGuildSettingsAsync(new GuildSettingsDTO(guild.GuildID.Value), ct).ConfigureAwait(false);
                if (!guildSettingsResult.IsSuccess && !(guildSettingsResult.Error is HttpStatusCodeError er && er.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold guild settings database objects: {error}", guildSettingsResult.Error);
                    _appLifetime.StopApplication();
                }

                Result<GuildTwitterSettingsDTO> guildTwitterSettingsResult = await _dbApi.CreateGuildTwitterSettingsAsync(new GuildTwitterSettingsDTO(guild.GuildID.Value), ct).ConfigureAwait(false);
                if (!guildTwitterSettingsResult.IsSuccess && !(guildTwitterSettingsResult.Error is HttpStatusCodeError er2 && er2.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold guild twitter settings database objects: {error}", guildSettingsResult.Error);
                    _appLifetime.StopApplication();
                }

                Result<PlanetsideSettingsDTO> planetsideSettingsResult = await _dbApi.CreatePlanetsideSettingsAsync(new PlanetsideSettingsDTO(guild.GuildID.Value), ct).ConfigureAwait(false);
                if (!planetsideSettingsResult.IsSuccess && !(planetsideSettingsResult.Error is HttpStatusCodeError er3 && er3.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not scaffold PlanetSide settings database objects: {error}", guildSettingsResult.Error);
                    _appLifetime.StopApplication();
                }

                Result<GuildWelcomeMessageDto> guildWelcomeMessageResult = await _dbApi.CreateGuildWelcomeMessageAsync(
                    new GuildWelcomeMessageDto(guild.GuildID.Value)
                    {
                        IsEnabled = false
                    },
                    ct).ConfigureAwait(false);

                if (!guildWelcomeMessageResult.IsSuccess && !(planetsideSettingsResult.Error is HttpStatusCodeError er4 && er4.StatusCode == HttpStatusCode.Conflict))
                {
                    _logger.LogCritical("Could not initialise guild welcome message database objects: {error}", guildWelcomeMessageResult.Error);
                    _appLifetime.StopApplication();
                }
            }
        }
    }
}
