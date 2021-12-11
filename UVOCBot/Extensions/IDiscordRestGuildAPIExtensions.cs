using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Remora.Discord.API.Abstractions.Rest;

public static class IDiscordRestGuildAPIExtensions
{
    private const int MAX_MEMBER_PAGE_SIZE = 1000;

    /// <summary>
    /// Gets all the members of a guild.
    /// </summary>
    /// <param name="guildApi"></param>
    /// <param name="guildID">The guild to list the members of.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<Result<IReadOnlyList<IGuildMember>>> GetAllMembersAsync
    (
        this IDiscordRestGuildAPI guildApi,
        Snowflake guildID,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (Result<IReadOnlyList<IGuildMember>> element in GetAllMembersAsync(guildApi, guildID, (_) => true, ct).ConfigureAwait(false))
            yield return element;
    }

    /// <summary>
    /// Gets all the members of a guild.
    /// </summary>
    /// <param name="guildApi"></param>
    /// <param name="guildID">The guild to list the members of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<Result<IReadOnlyList<IGuildMember>>> GetAllMembersAsync(
        this IDiscordRestGuildAPI guildApi,
        Snowflake guildID,
        Func<IGuildMember, bool> predicate,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Result<IReadOnlyList<IGuildMember>> members;
        Snowflake afterID = new(0);

        do
        {
            members = await guildApi.ListGuildMembersAsync(guildID, MAX_MEMBER_PAGE_SIZE, afterID, ct).ConfigureAwait(false);

            if (!members.IsSuccess)
            {
                yield return members;
                yield break;
            }
            else
            {
                yield return members.Entity.Where(predicate).ToList().AsReadOnly();
            }

            afterID = members.Entity.Max(u =>
            {
                if (u.User.HasValue)
                    return u.User.Value.ID;
                else
                    return new Snowflake(0, Constants.DiscordEpoch);
            });
        } while (members.Entity.Count == MAX_MEMBER_PAGE_SIZE);
    }
}
