using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using System.Collections.Generic;

namespace UVOCBot.Discord.Core.Abstractions.Services;

/// <summary>
/// A cache for <see cref="IVoiceState"/> objects, keyed by the value of <see cref="IVoiceState.UserID"/>
/// </summary>
public interface IVoiceStateCacheService
{
    /// <summary>
    /// Sets a voice state within the cache. If there is no <see cref="IVoiceState.ChannelID"/> present in the state,
    /// then if a state already exists in the cache for the <see cref="IVoiceState.UserID"/> it will be removed.
    /// </summary>
    /// <param name="state">The voice state update to cache.</param>
    void Set(IVoiceState state);

    /// <summary>
    /// Gets the <see cref="IVoiceState"/> of a user
    /// </summary>
    /// <param name="userID"></param>
    /// <returns></returns>
    Optional<IVoiceState> GetUserVoiceState(Snowflake userID);

    /// <summary>
    /// Gets the <see cref="IVoiceState"/> for all users in a channel
    /// </summary>
    /// <param name="channelID"></param>
    /// <returns></returns>
    Optional<IReadOnlyList<IVoiceState>> GetChannelVoiceStates(Snowflake channelID);

    /// <summary>
    /// Removes a user's <see cref="IVoiceState"/>
    /// </summary>
    /// <param name="userID"></param>
    void Remove(Snowflake userID);
}
