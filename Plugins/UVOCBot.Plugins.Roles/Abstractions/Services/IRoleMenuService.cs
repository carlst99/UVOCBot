using Remora.Discord.API.Abstractions.Objects;
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
    /// <param name="messageID">The ID of the role menu message.</param>
    /// <param name="menu">The retrieved menu, or null if the retrieval failed.</param>
    /// <returns>A value indicating whether or not the menu was successfully retrieved.</returns>
    bool TryGetGuildRoleMenu(ulong messageID, [NotNullWhen(true)] out GuildRoleMenu? menu);

    /// <summary>
    /// Checks that a role menu exists as a message.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing the message on success.</returns>
    Task<Result<IMessage>> CheckRoleMenuMessageExistsAsync(GuildRoleMenu menu, CancellationToken ct = default);

    /// <summary>
    /// Creates an embed that displays a role menu.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <returns>The display embed.</returns>
    IEmbed CreateRoleMenuEmbed(GuildRoleMenu menu);

    /// <summary>
    /// Updates a role menu message to match the stored representation.
    /// </summary>
    /// <param name="menu">The menu to update.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation, and containing the updated message on success.</returns>
    Task<Result<IMessage>> UpdateRoleMenuMessageAsync(GuildRoleMenu menu, CancellationToken ct = default);
}
