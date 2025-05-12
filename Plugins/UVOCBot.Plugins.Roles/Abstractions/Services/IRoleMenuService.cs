using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Plugins.Roles.Abstractions.Services;

/// <summary>
/// Represents an interface to assist with working with role menus.
/// </summary>
public interface IRoleMenuService
{
    /// <summary>
    /// Attempts to retrieve a <see cref="GuildRoleMenu"/> object from the database.
    /// </summary>
    /// <param name="guildId">The ID of the guild in which the role menu is present.</param>
    /// <param name="messageID">The ID of the role menu message.</param>
    /// <param name="menu">The retrieved menu, or null if the retrieval failed.</param>
    /// <returns>A value indicating whether the menu was successfully retrieved.</returns>
    bool TryGetGuildRoleMenu
    (
        Optional<Snowflake> guildId,
        ulong messageID,
        [NotNullWhen(true)] out GuildRoleMenu? menu
    );

    /// <summary>
    /// Updates a role menu message to match the stored representation. This method may update properties on the
    /// <paramref name="menu"/>, so ensure that changes get persisted to the database.
    /// </summary>
    /// <param name="menu">The menu to update.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing the updated message on success.</returns>
    Task<Result<IMessage>> UpdateRoleMenuMessageAsync(GuildRoleMenu menu, CancellationToken ct = default);
}
