using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IWelcomeMessageService
    {
        Task<Result> SendWelcomeMessage(IGuildMemberAdd gatewayEvent, CancellationToken ct = default);
        Task<Result> SetAlternateRoles(IInteractionCreate gatewayEvent, CancellationToken ct = default);
        Task<Result> SetNicknameFromGuess(IInteractionCreate gatewayEvent, CancellationToken ct = default);
        Task<Result> InformNicknameNoMatch(IInteractionCreate gatewayEvent, CancellationToken ct = default);
    }
}
