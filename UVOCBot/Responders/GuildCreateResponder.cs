using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;

namespace UVOCBot.Responders;

public class GuildCreateResponder : IResponder<IGuildCreate>
{
    private readonly IVoiceStateCacheService _cache;

    public GuildCreateResponder(IVoiceStateCacheService cache)
    {
        _cache = cache;
    }

    public Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = default)
    {
        if (!gatewayEvent.Guild.TryPickT0(out IGuildCreate.IAvailableGuild guild, out _))
            return Task.FromResult(Result.FromSuccess());

        foreach (IPartialVoiceState voiceState in guild.VoiceStates.Where(v => v.ChannelID.HasValue))
        {
            // We can assume each value is present because the IGuildCreate event
            // is guaranteed to contain complete voice state payloads.
            VoiceState trueState = new
            (
                guild.ID,
                voiceState.ChannelID.Value,
                voiceState.UserID.Value,
                voiceState.Member,
                voiceState.SessionID.Value,
                voiceState.IsDeafened.Value,
                voiceState.IsMuted.Value,
                voiceState.IsSelfDeafened.Value,
                voiceState.IsSelfMuted.Value,
                voiceState.IsStreaming,
                voiceState.IsVideoEnabled.Value,
                voiceState.IsSuppressed.Value,
                null
            );

            _cache.Set(trueState);
        }

        return Task.FromResult(Result.FromSuccess());
    }
}
