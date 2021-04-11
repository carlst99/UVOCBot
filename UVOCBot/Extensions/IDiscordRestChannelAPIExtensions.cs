using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Remora.Discord.API.Abstractions.Rest
{
    public static class IDiscordRestChannelAPIExtensions
    {
        private const int MAX_REACTION_PAGE_SIZE = 100;

        /// <summary>
        /// Gets all the users who reacted to a message
        /// </summary>
        /// <param name="channelAPI"></param>
        /// <param name="channelID">The ID of the channel that the message was sent in</param>
        /// <param name="messageID">The ID of the message which has the reaction</param>
        /// <param name="emoji">The emoji to count reactions for</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async IAsyncEnumerable<Result<IReadOnlyList<IUser>>> GetAllReactorsAsync(
            this IDiscordRestChannelAPI channelAPI,
            Snowflake channelID,
            Snowflake messageID,
            string emoji,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            Result<IReadOnlyList<IUser>> reactions;
            Snowflake afterID = new(0);

            do
            {
                reactions = await channelAPI.GetReactionsAsync(channelID, messageID, emoji, after: afterID, limit: MAX_REACTION_PAGE_SIZE, ct: ct).ConfigureAwait(false);
                yield return reactions;

                if (!reactions.IsSuccess)
                    yield break;

                afterID = reactions.Entity.Max(u => u.ID);
            } while (reactions.Entity.Count == MAX_REACTION_PAGE_SIZE);
        }
    }
}
