using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class GuildMemberAddResponder : IResponder<IGuildMemberAdd>
    {
        private readonly IWelcomeMessageService _welcomeMessageService;

        public GuildMemberAddResponder(
            IWelcomeMessageService welcomeMessageService)
        {
            _welcomeMessageService = welcomeMessageService;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            return await _welcomeMessageService.SendWelcomeMessage(gatewayEvent, ct).ConfigureAwait(false);
        }
    }
}
