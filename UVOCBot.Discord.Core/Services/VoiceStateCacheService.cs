﻿using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using System.Collections.Generic;
using UVOCBot.Discord.Core.Abstractions.Services;

namespace UVOCBot.Discord.Core.Services;

/// <summary>
/// A cache for <see cref="IVoiceState"/> objects, keyed by the value of <see cref="IVoiceState.UserID"/>
/// </summary>
public sealed class VoiceStateCacheService : IVoiceStateCacheService
{
    private readonly Dictionary<Snowflake, IVoiceState> _userVoiceStates = [];
    private readonly Dictionary<Snowflake, List<Snowflake>> _channelUsers = [];

    /// <inheritdoc/>
    public Optional<IReadOnlyList<IVoiceState>> GetChannelVoiceStates(Snowflake channelID)
    {
        List<IVoiceState> states = [];

        if (!_channelUsers.TryGetValue(channelID, out List<Snowflake>? channelUser))
            return new Optional<IReadOnlyList<IVoiceState>>();

        foreach (Snowflake user in channelUser)
            states.Add(_userVoiceStates[user]);

        return states.AsReadOnly();
    }

    /// <inheritdoc/>
    public Optional<IVoiceState> GetUserVoiceState(Snowflake userID)
    {
        return _userVoiceStates.TryGetValue(userID, out IVoiceState? state)
            ? new Optional<IVoiceState>(state)
            : new Optional<IVoiceState>();
    }

    /// <inheritdoc/>
    public void Remove(Snowflake userID)
    {
        if (!_userVoiceStates.Remove(userID, out IVoiceState? state))
            return;

        RemoveChannelUser(state);
    }

    /// <inheritdoc/>
    public void Set(IVoiceState state)
    {
        // We only want to store voice states in which the user is actively in a channel
        if (!state.ChannelID.HasValue)
        {
            Remove(state.UserID);
            return;
        }

        // If this state has previously been cached, we want to update the channel link
        if (_userVoiceStates.TryGetValue(state.UserID, out IVoiceState? voiceState))
            RemoveChannelUser(voiceState);

        _userVoiceStates[state.UserID] = state;

        if (!_channelUsers.ContainsKey(state.ChannelID.Value))
            _channelUsers.Add(state.ChannelID.Value, new List<Snowflake>());

        _channelUsers[state.ChannelID.Value].Add(state.UserID);
    }

    private void RemoveChannelUser(IVoiceState state)
    {
        Snowflake channelID = state.ChannelID!.Value;

        _channelUsers[channelID].Remove(state.UserID);
        if (_channelUsers[channelID].Count == 0)
            _channelUsers.Remove(channelID);
    }
}
