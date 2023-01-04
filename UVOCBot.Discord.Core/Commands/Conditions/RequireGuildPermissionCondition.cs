using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Discord.Core.Commands.Conditions;

/// <summary>
/// Checks required guild permissions before allowing execution.
/// </summary>
public class RequireGuildPermissionCondition : ICondition<RequireGuildPermissionAttribute>
{
    private readonly IInteraction _context;
    private readonly IPermissionChecksService _permissionChecksService;

    public RequireGuildPermissionCondition
    (
        IInteractionContext context,
        IPermissionChecksService permissionChecksService
    )
    {
        _context = context.Interaction;
        _permissionChecksService = permissionChecksService;
    }

    /// <inheritdoc />
    public async ValueTask<Result> CheckAsync(RequireGuildPermissionAttribute attribute, CancellationToken ct = default)
    {
        if (!_context.GuildID.HasValue)
            return new ContextError(ContextError.GuildTextChannels);

        if (!_context.ChannelID.HasValue)
            return new ArgumentInvalidError("channel", "No channel was present");

        if (!_context.TryGetUser(out IUser? user))
            return new ArgumentInvalidError("user", "No user was present");

        if (attribute.IncludeSelf)
        {
            Result selfPermissionCheck = await DoPermissionCheck(attribute.RequiredPermissions, DiscordConstants.UserId, ct)
                .ConfigureAwait(false);
            if (!selfPermissionCheck.IsSuccess)
                return selfPermissionCheck;
        }

        return await DoPermissionCheck(attribute.RequiredPermissions, user.ID, ct);
    }

    private async Task<Result> DoPermissionCheck
    (
        IEnumerable<DiscordPermission> permissions,
        Snowflake userID,
        CancellationToken ct = default
    )
    {
        Result<IDiscordPermissionSet> getPermissions = await _permissionChecksService.GetPermissionsInChannel
        (
            _context.ChannelID.Value,
            userID,
            ct
        ).ConfigureAwait(false);

        if (!getPermissions.IsDefined(out IDiscordPermissionSet? permissionSet))
            return Result.FromError(getPermissions);

        List<DiscordPermission> missingPermissions = permissions
            .Where(p => !permissionSet.HasAdminOrPermission(p))
            .ToList();

        return missingPermissions.Count > 0
            ? new PermissionError(missingPermissions, userID, _context.ChannelID.Value)
            : Result.FromSuccess();
    }
}
