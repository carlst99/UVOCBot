using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using System.Collections.Generic;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Discord.Core.Services;

/// <summary>
/// A cache for <see cref="IVoiceState"/> objects, keyed by the value of <see cref="IVoiceState.UserID"/>
/// </summary>
public sealed class VoiceStateCacheService : IVoiceStateCacheService
{
    private readonly Dictionary<Snowflake, IVoiceState> _userVoiceStates;
    private readonly Dictionary<Snowflake, List<Snowflake>> _channelUsers;

    public VoiceStateCacheService()
    {
        _userVoiceStates = new Dictionary<Snowflake, IVoiceState>();
        _channelUsers = new Dictionary<Snowflake, List<Snowflake>>();
    }

    /// <inheritdoc/>
    public Optional<IReadOnlyList<IVoiceState>> GetChannelVoiceStates(Snowflake channelID)
    {
        List<IVoiceState> states = new();

        if (_channelUsers.ContainsKey(channelID))
        {
            foreach (Snowflake user in _channelUsers[channelID])
                states.Add(_userVoiceStates[user]);

            return states.AsReadOnly();
        }
        else
        {
            return new Optional<IReadOnlyList<IVoiceState>>();
        }
    }

    /// <inheritdoc/>
    public Optional<IVoiceState> GetUserVoiceState(Snowflake userID)
    {
        if (_userVoiceStates.ContainsKey(userID))
            return new Optional<IVoiceState>(_userVoiceStates[userID]);
        else
            return new Optional<IVoiceState>();
    }

    /// <inheritdoc/>
    public void Remove(Snowflake userID)
    {
        if (_userVoiceStates.ContainsKey(userID))
        {
            IVoiceState state = _userVoiceStates[userID];
            _userVoiceStates.Remove(userID);

            RemoveChannelUser(state);
        }
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
        if (_userVoiceStates.ContainsKey(state.UserID))
            RemoveChannelUser(_userVoiceStates[state.UserID]);

        _userVoiceStates[state.UserID] = state;

        if (!_channelUsers.ContainsKey(state.ChannelID.Value))
            _channelUsers.Add(state.ChannelID.Value, new List<Snowflake>());

        _channelUsers[state.ChannelID.Value].Add(state.UserID);
    }

    private void RemoveChannelUser(IVoiceState state)
    {
#pragma warning disable CS8629 // Nullable value type may be null.
        Snowflake channelID = state.ChannelID.Value;
#pragma warning restore CS8629 // Nullable value type may be null.

        _channelUsers[channelID].Remove(state.UserID);
        if (_channelUsers[channelID].Count == 0)
            _channelUsers.Remove(channelID);
    }
}
