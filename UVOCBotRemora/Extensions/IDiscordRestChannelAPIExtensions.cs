using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Remora.Discord.API.Abstractions.Rest
{
    public static class IDiscordRestChannelAPIExtensions
    {
        private const int MAX_REACTION_PAGE_SIZE = 100;

        // TODO: Convert to enumerator. This means that we aren't bringing huge lists into memory for large guilds
        public static async Task<Result<IReadOnlyList<IUser>>> GetAllReactionsAsync(
            this IDiscordRestChannelAPI channelAPI,
            Snowflake channelID,
            Snowflake messageID,
            string emoji,
            CancellationToken ct = default)
        {
            List<IUser> users = new();
            Result res = await GetAllReactionsRecursiveAsync(channelAPI, users, channelID, messageID, emoji, ct: ct).ConfigureAwait(false);

            return res.IsSuccess
                ? Result<IReadOnlyList<IUser>>.FromSuccess(users.AsReadOnly())
                : Result<IReadOnlyList<IUser>>.FromError(res);
        }

        private static async Task<Result> GetAllReactionsRecursiveAsync(
            IDiscordRestChannelAPI channelAPI,
            List<IUser> usersOut,
            Snowflake channelID,
            Snowflake messageID,
            string emoji,
            Optional<Snowflake> lastUserId = default,
            CancellationToken ct = default)
        {
            Result<IReadOnlyList<IUser>> reactions = await channelAPI.GetReactionsAsync(channelID, messageID, emoji, after: lastUserId, limit: 100, ct: ct).ConfigureAwait(false);

            if (!reactions.IsSuccess)
                Result.FromError(reactions);

#nullable disable
            usersOut.AddRange(reactions.Entity);
#nullable restore

            if (reactions.Entity.Count % MAX_REACTION_PAGE_SIZE != 0)
            {
                Result recursiveRes = await GetAllReactionsRecursiveAsync(channelAPI, usersOut, channelID, messageID, emoji, reactions.Entity.Max(u => u.ID), ct).ConfigureAwait(false);
                if (!recursiveRes.IsSuccess)
                    return recursiveRes;
            }

            return Result.FromSuccess();
        }
    }
}
