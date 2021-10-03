using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Discord.Core.Commands.Conditions
{
    /// <summary>
    /// Checks required guild permissions before allowing execution.
    /// </summary>
    public class RequireGuildPermissionCondition : ICondition<RequireGuildPermissionAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IPermissionChecksService _permissionChecksService;

        public RequireGuildPermissionCondition
        (
            ICommandContext context,
            IPermissionChecksService permissionChecksService
        )
        {
            _context = context;
            _permissionChecksService = permissionChecksService;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequireGuildPermissionAttribute attribute, CancellationToken ct = default)
        {
            if (!_context.GuildID.HasValue)
                return new ContextError(ChannelContext.Guild);

            if (attribute.IncludeCurrent)
            {
                Result selfPermissionCheck = await DoPermissionCheck(attribute.Permission, DiscordConstants.UserId, ct).ConfigureAwait(false);
                if (!selfPermissionCheck.IsSuccess)
                    return selfPermissionCheck;
            }

            return await DoPermissionCheck(attribute.Permission, _context.User.ID, ct).ConfigureAwait(false);
        }

        private async Task<Result> DoPermissionCheck(DiscordPermission permission, Snowflake userID, CancellationToken ct = default)
        {
            Result<IDiscordPermissionSet> getPermissions = await _permissionChecksService.GetPermissionsInChannel
            (
                _context.ChannelID,
                userID,
                ct
            ).ConfigureAwait(false);

            if (!getPermissions.IsDefined())
                return Result.FromError(getPermissions);

            if (!getPermissions.Entity.HasAdminOrPermission(permission))
                return new PermissionError(permission, _context.User.ID, _context.ChannelID);

            return Result.FromSuccess();
        }
    }
}
