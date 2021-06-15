using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IWelcomeMessageService
    {
        Task<Result> SendWelcomeMessage(IGuildMemberAdd gatewayEvent, CancellationToken ct = default);
    }
}
