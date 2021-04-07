using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBotRemora.Services;

namespace UVOCBotRemora.Responders
{
    public class VoiceStateUpdateResponder : IResponder<IVoiceStateUpdate>
    {
        private readonly IVoiceStateCacheService _cache;

        public VoiceStateUpdateResponder(IVoiceStateCacheService memoryCache)
        {
            _cache = memoryCache;
        }

        public Task<Result> RespondAsync(IVoiceStateUpdate gatewayEvent, CancellationToken ct = default)
        {
            _cache.Set(gatewayEvent);
            return Task.FromResult(Result.FromSuccess());
        }
    }
}
