﻿using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Services.Abstractions;

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
        if (gatewayEvent.VoiceStates.HasValue)
        {
            foreach (IPartialVoiceState voiceState in gatewayEvent.VoiceStates.Value.Where(v => v.ChannelID.HasValue))
            {
                VoiceState trueState = new
                (
                    gatewayEvent.ID,
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
        }

        // TODO: Do we need to do any DB scaffolding here?

        return Task.FromResult(Result.FromSuccess());
    }
}
