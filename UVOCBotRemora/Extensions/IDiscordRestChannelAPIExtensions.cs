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

        public static async IAsyncEnumerable<Result<IReadOnlyList<IUser>>> GetAllReactionsAsync(
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
