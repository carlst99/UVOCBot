using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Discord.Core.Services.Abstractions;

public interface IPermissionChecksService
{
    /// <summary>
    /// Ensures that this bot can manipulate the given roles.
    /// </summary>
    /// <param name="guildId">The guild in which the role manipulation will take place.</param>
    /// <param name="roleIds">The roles to be manipulated.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A result that may or may not have been successful.</returns>
    Task<IResult> CanManipulateRoles(Snowflake guildId, IEnumerable<ulong> roleIds, CancellationToken ct = default);

    /// <summary>
    /// Gets the permission set of a user in a given channel.
    /// </summary>
    /// <param name="channelId">The channel used to calculate the permission set.</param>
    /// <param name="userId">The user to get the permissions of.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>The user's permission set.</returns>
    Task<Result<IDiscordPermissionSet>> GetPermissionsInChannel(Snowflake channelId, Snowflake userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the permission set of a user in a given channel.
    /// </summary>
    /// <param name="channel">The channel used to calculate the permission set.</param>
    /// <param name="userId">The user to get the permissions of.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>The user's permission set.</returns>
    Task<Result<IDiscordPermissionSet>> GetPermissionsInChannel(IChannel channel, Snowflake userId, CancellationToken ct = default);
}
