using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Results;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;

namespace UVOCBot.Commands.Conditions
{
    /// <summary>
    /// Checks required Guild permissions before allowing execution.
    /// <remarks>Fails if the command is executed outside of a Guild./></remarks>
    /// </summary>
    public class RequireGuildPermissionCondition : ICondition<RequireGuildPermissionAttribute>
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly MessageResponseHelpers _responder;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireGuildPermissionCondition"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channelApi">The channel API.</param>
        /// <param name="guildApi">The guild API.</param>
        /// <param name="responder">The message responder.</param>
        public RequireGuildPermissionCondition(
            ICommandContext context,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi,
            MessageResponseHelpers responder)
        {
            _context = context;
            _channelApi = channelApi;
            _guildApi = guildApi;
            _responder = responder;
        }

        /// <inheritdoc />
        public async ValueTask<Result> CheckAsync(RequireGuildPermissionAttribute attribute, CancellationToken ct = default)
        {
            // Get and check the channel
            Result<IChannel> getChannelResult = await _channelApi.GetChannelAsync(_context.ChannelID, ct).ConfigureAwait(false);
            if (!getChannelResult.IsSuccess)
                return Result.FromError(getChannelResult);

            IChannel channel = getChannelResult.Entity;
            if (!channel.GuildID.HasValue)
            {
                await _responder.RespondWithUserErrorAsync(_context, "This command must be executed in a guild.", ct).ConfigureAwait(false);
                return new ConditionNotSatisfiedError("Command requires a guild permission but was executed outside of a guild.");
            }

            var guildId = channel.GuildID.Value;

            // Get and check the guild roles
            Result<IReadOnlyList<IRole>> getGuildRolesResult = await _guildApi.GetGuildRolesAsync(guildId, ct).ConfigureAwait(false);
            if (!getGuildRolesResult.IsSuccess)
                return Result.FromError(getGuildRolesResult);

            IReadOnlyList<IRole> guildRoles = getGuildRolesResult.Entity;
            var everyoneRole = guildRoles.FirstOrDefault(r => r.ID == guildId);
            if (everyoneRole is null)
                return new Exception("No @everyone role found.");

            // Get and check the executing guild member
            Result<IGuildMember> getGuildMemberResult = await _guildApi.GetGuildMemberAsync(guildId, _context.User.ID, ct).ConfigureAwait(false);
            if (!getGuildMemberResult.IsSuccess)
                return Result.FromError(getGuildMemberResult);

            if (getGuildMemberResult.Entity is null)
                return new Exception("Executing member not found");
            IGuildMember member = getGuildMemberResult.Entity;

            // Get the current (bot) member
            Result<IGuildMember> getCurrentMemberResult = await _guildApi.GetGuildMemberAsync(guildId, BotConstants.UserId, ct).ConfigureAwait(false);
            if (!getCurrentMemberResult.IsSuccess)
                return Result.FromError(getCurrentMemberResult);

            if (getCurrentMemberResult.Entity is null)
                return new Exception("Current member not found");
            IGuildMember currentMember = getCurrentMemberResult.Entity;

            // Get the guild
            Result<IGuild> getGuildResult = await _guildApi.GetGuildAsync(guildId, ct: ct).ConfigureAwait(false);
            if (!getGuildResult.IsSuccess)
                return Result.FromError(getGuildResult);

            // If required, check that the bot has the required permission
            if (attribute.IncludeCurrent)
            {
                Result hasPermissionsResult = CheckMemberPermissions(guildRoles, currentMember, BotConstants.UserId, channel, everyoneRole, attribute.Permission);
                if (!hasPermissionsResult.IsSuccess)
                {
                    await _responder.RespondWithUserErrorAsync(
                        _context,
                        $"<@{ BotConstants.UserId }> (that's me!) needs the { Formatter.InlineQuote(attribute.Permission.ToString()) } permission in this channel to perform this action.",
                        ct).ConfigureAwait(false);

                    return hasPermissionsResult;
                }
            }

            // Succeed if the user is the Owner of the guild, else check the permission of the executing member
            Snowflake guildOwnerId = getGuildResult.Entity.OwnerID;
            if (guildOwnerId.Equals(_context.User.ID))
            {
                return Result.FromSuccess();
            }
            else
            {
                Result hasPermissionsResult = CheckMemberPermissions(guildRoles, member, _context.User.ID, channel, everyoneRole, attribute.Permission);
                if (!hasPermissionsResult.IsSuccess)
                {
                    await _responder.RespondWithUserErrorAsync(
                        _context,
                        $"You need the { Formatter.InlineQuote(attribute.Permission.ToString()) } permission in this channel to use this command.",
                        ct).ConfigureAwait(false);

                    return hasPermissionsResult;
                }

                return Result.FromSuccess();
            }
        }

        private static Result CheckMemberPermissions(IReadOnlyList<IRole> guildRoles, IGuildMember member, Snowflake userId, IChannel channel, IRole everyoneRole, DiscordPermission permission)
        {
            List<IRole> memberRoles = guildRoles.Where(r => member.Roles.Contains(r.ID)).ToList();
            IDiscordPermissionSet computedPermissions;

            if (channel.PermissionOverwrites.HasValue)
            {
                computedPermissions = DiscordPermissionSet.ComputePermissions(
                    userId, everyoneRole, memberRoles, channel.PermissionOverwrites.Value
                );
            }
            else
            {
                computedPermissions = DiscordPermissionSet.ComputePermissions(
                    userId, everyoneRole, memberRoles
                );
            }

            // Succeed if the user is an Administrator of the guild
            if (computedPermissions.HasPermission(DiscordPermission.Administrator))
                return Result.FromSuccess();

            bool hasPermission = computedPermissions.HasPermission(permission);
            return !hasPermission
                ? new ConditionNotSatisfiedError($"Guild User requesting the command does not have the required { permission } permission")
                : Result.FromSuccess();
        }
    }
}
