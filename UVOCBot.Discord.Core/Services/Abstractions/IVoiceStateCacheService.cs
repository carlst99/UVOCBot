using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;

namespace UVOCBot.Discord.Core.Services.Abstractions
{
    /// <summary>
    /// A cache for <see cref="IVoiceState"/> objects, keyed by the value of <see cref="IVoiceState.UserID"/>
    /// </summary>
    public interface IVoiceStateCacheService
    {
        /// <summary>
        /// Sets a voice state within the cache. If there is no channel in the state, a previously cached state with the same member will be removed
        /// </summary>
        /// <param name="state"></param>
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
}
