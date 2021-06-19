using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class PermissionChecksService : IPermissionChecksService
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly MessageResponseHelpers _responder;

        public PermissionChecksService(
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

        public async Task<IResult> CanManipulateRoles(Snowflake guildId, IEnumerable<ulong> roleIds, CancellationToken ct = default)
        {
            Result<IReadOnlyList<IRole>> getGuildRoles = await _guildApi.GetGuildRolesAsync(guildId, ct).ConfigureAwait(false);
            if (!getGuildRoles.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", ct).ConfigureAwait(false);
                return getGuildRoles;
            }

            Result<IGuildMember> getCurrentMember = await _guildApi.GetGuildMemberAsync(_context.GuildID.Value, BotConstants.UserId, ct).ConfigureAwait(false);
            if (!getCurrentMember.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", ct).ConfigureAwait(false);
                return getCurrentMember;
            }

            // Enumerate roles in order from highest to lowest ranked, so that logically the first role in the current member's role list is its highest
            IRole? highestRole = null;
            foreach (IRole role in getGuildRoles.Entity.OrderByDescending(r => r.Position))
            {
                if (getCurrentMember.Entity.Roles.Contains(role.ID))
                {
                    highestRole = role;
                    break;
                }
            }

            if (highestRole is null)
                return await _responder.RespondWithUserErrorAsync(_context, "I cannot assign these roles, as I do not have a role myself.", ct).ConfigureAwait(false);

            // Check that each role is assignable by us
            foreach (ulong roleId in roleIds)
            {
                if (!getGuildRoles.Entity.Any(r => r.ID.Value == roleId))
                    return await _responder.RespondWithUserErrorAsync(_context, "A supplied role does not exist.", ct).ConfigureAwait(false);

                IRole role = getGuildRoles.Entity.First(r => r.ID.Value == roleId);
                if (role.Position > highestRole.Position)
                {
                    return await _responder.RespondWithUserErrorAsync(
                        _context,
                        $"I cannot assign the { Formatter.RoleMention(role.ID) } role, as it is positioned above my own highest role.",
                        ct).ConfigureAwait(false);
                }
            }

            return Result.FromSuccess();
        }

        public async Task<Result<IDiscordPermissionSet>> GetPermissionsInChannel(Snowflake channelId, Snowflake userId, CancellationToken ct = default)
        {
            // Get and check the channel
            Result<IChannel> getChannelResult = await _channelApi.GetChannelAsync(_context.ChannelID, ct).ConfigureAwait(false);
            if (!getChannelResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(getChannelResult);

            IChannel channel = getChannelResult.Entity;
            if (!channel.GuildID.HasValue)
            {
                await _responder.RespondWithUserErrorAsync(_context, "This command must be executed in a guild.", ct).ConfigureAwait(false);
                return new Exception("Command requires a guild permission but was executed outside of a guild.");
            }

            Snowflake guildId = channel.GuildID.Value;

            // Get the guild
            Result<IGuild> getGuildResult = await _guildApi.GetGuildAsync(guildId, ct: ct).ConfigureAwait(false);
            if (!getGuildResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(getGuildResult);

            IReadOnlyList<IRole> guildRoles = getGuildResult.Entity.Roles;
            IRole? everyoneRole = guildRoles.FirstOrDefault(r => r.ID == guildId);
            if (everyoneRole is null)
                return new Exception("No @everyone role found.");

            // Get and check the executing guild member
            Result<IGuildMember> getGuildMemberResult = await _guildApi.GetGuildMemberAsync(guildId, _context.User.ID, ct).ConfigureAwait(false);
            if (!getGuildMemberResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(getGuildMemberResult);

            if (getGuildMemberResult.Entity is null)
                return new Exception("Executing member not found");
            IGuildMember member = getGuildMemberResult.Entity;

            List<IRole> guildMemberRoles = getGuildResult.Entity.Roles.Where(r => getGuildMemberResult.Entity.Roles.Contains(r.ID)).ToList();

            // Compute the final permissions
            if (channel.PermissionOverwrites.HasValue)
                return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userId, everyoneRole, guildMemberRoles, channel.PermissionOverwrites.Value));
            else
                return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userId, everyoneRole, guildMemberRoles));
        }
    }
}
