using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class GuildMemberAddResponder : IResponder<IGuildMemberAdd>
    {
        private readonly ILogger<GuildMemberAddResponder> _logger;
        private readonly IDbApiService _dbApi;

        public GuildMemberAddResponder(ILogger<GuildMemberAddResponder> logger, IDbApiService dbApi)
        {
            _logger = logger;
            _dbApi = dbApi;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            Result<GuildWelcomeMessageDto> welcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(gatewayEvent.GuildID.Value, ct).ConfigureAwait(false);
            if (!welcomeMessage.IsSuccess)
            {
                _logger.LogError("Failed to retrieve GuildWelcomeMessage object: {error}", welcomeMessage.Error);
                return Result.FromError(welcomeMessage);
            }
        }
    }
}
