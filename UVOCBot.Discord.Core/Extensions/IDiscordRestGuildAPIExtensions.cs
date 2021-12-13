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
    public static async IAsyncEnumerable<Result<IReadOnlyList<IGuildMember>>> GetAllMembersAsync
    (
        this IDiscordRestGuildAPI guildApi,
        Snowflake guildID,
        Func<IGuildMember, bool> predicate,
        [EnumeratorCancellation] CancellationToken ct = default
    )
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
        }
        while (members.Entity.Count == MAX_MEMBER_PAGE_SIZE);
    }

    /// <summary>
    /// Modifies the roles of a guild member.
    /// </summary>
    /// <param name="guildApi">The guild API.</param>
    /// <param name="guildId">The guild that the member is part of.</param>
    /// <param name="userId">The user to assign the roles to.</param>
    /// <param name="currentRoles">The user's existing roles. Optional.</param>
    /// <param name="rolesToAdd">The roles to add.</param>
    /// <param name="rolesToAdd">The roles to remove.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    public static async Task<Result> ModifyRoles
    (
        this IDiscordRestGuildAPI guildApi,
        Snowflake guildId,
        Snowflake userId,
        IReadOnlyList<Snowflake>? currentRoles,
        IEnumerable<ulong>? rolesToAdd = null,
        IEnumerable<ulong>? rolesToRemove = null,
        CancellationToken ct = default
    )
    {
        List<Snowflake>? newRoles;

        if (currentRoles is null)
        {
            Result<IGuildMember> memberResult = await guildApi.GetGuildMemberAsync(guildId, userId, ct).ConfigureAwait(false);
            if (!memberResult.IsDefined(out IGuildMember? member))
                return Result.FromError(memberResult);

            newRoles = new List<Snowflake>(member.Roles);
        }
        else
        {
            newRoles = new List<Snowflake>(currentRoles);
        }

        if (rolesToAdd is not null)
        {
            foreach (ulong roleId in rolesToAdd)
            {
                Snowflake snowflake = new(roleId);
                if (!newRoles.Contains(snowflake))
                    newRoles.Add(snowflake);
            }
        }

        if (rolesToRemove is not null)
        {
            foreach (ulong roleId in rolesToRemove)
            {
                Snowflake snowflake = new(roleId);
                if (newRoles.Contains(snowflake))
                    newRoles.Remove(snowflake);
            }
        }

        return await guildApi.ModifyGuildMemberAsync(guildId, userId, roles: newRoles, ct: ct).ConfigureAwait(false);
    }
}
