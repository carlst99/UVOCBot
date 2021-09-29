using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;

// TODO: Refactor feedback service out of this class
// Consumers should be responsible for feedback instead
// Return a PermissionError with the reason for consumers to use.

namespace UVOCBot.Discord.Core.Services
{
    /// <inheritdoc cref="IPermissionChecksService"/>
    public class PermissionChecksService : IPermissionChecksService
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;

        public PermissionChecksService(
            ICommandContext context,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi)
        {
            _context = context;
            _channelApi = channelApi;
            _guildApi = guildApi;
        }

        /// <inheritdoc />
        public async Task<IResult> CanManipulateRoles(Snowflake guildId, IEnumerable<ulong> roleIds, CancellationToken ct = default)
        {
            Result<IReadOnlyList<IRole>> getGuildRoles = await _guildApi.GetGuildRolesAsync(guildId, ct).ConfigureAwait(false);
            if (!getGuildRoles.IsSuccess)
                return getGuildRoles;

            Result<IGuildMember> getCurrentMember = await _guildApi.GetGuildMemberAsync(_context.GuildID.Value, DiscordConstants.UserId, ct).ConfigureAwait(false);
            if (!getCurrentMember.IsSuccess)
                return getCurrentMember;

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
                return await _feedbackService.SendContextualErrorAsync("I cannot assign these roles, as I do not have a role myself.", ct: ct).ConfigureAwait(false);

            // Check that each role is assignable by us
            foreach (ulong roleId in roleIds)
            {
                if (!getGuildRoles.Entity.Any(r => r.ID.Value == roleId))
                    return await _feedbackService.SendContextualErrorAsync("A supplied role does not exist.", ct: ct).ConfigureAwait(false);

                IRole role = getGuildRoles.Entity.First(r => r.ID.Value == roleId);
                if (role.Position > highestRole.Position)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        $"I cannot assign the { Formatter.RoleMention(role.ID) } role, as it is positioned above my own highest role.",
                        ct: ct).ConfigureAwait(false);
                }
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result<IDiscordPermissionSet>> GetPermissionsInChannel(Snowflake channelId, Snowflake userId, CancellationToken ct = default)
        {
            // Get and check the channel
            Result<IChannel> getChannelResult = await _channelApi.GetChannelAsync(channelId, ct).ConfigureAwait(false);
            if (!getChannelResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(getChannelResult);

            return await GetPermissionsInChannel(getChannelResult.Entity, userId, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<IDiscordPermissionSet>> GetPermissionsInChannel(IChannel channel, Snowflake userId, CancellationToken ct = default)
        {
            // Check the channel belongs to a guild
            if (!channel.GuildID.HasValue)
                return new ContextError(Commands.Conditions.Attributes.ChannelContext.Guild);

            Snowflake guildId = channel.GuildID.Value;

            // Get the guild
            Result<IGuild> getGuildResult = await _guildApi.GetGuildAsync(guildId, ct: ct).ConfigureAwait(false);
            if (!getGuildResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(getGuildResult);

            // Get and check the guild member
            Result<IGuildMember> getGuildMemberResult = await _guildApi.GetGuildMemberAsync(guildId, userId, ct).ConfigureAwait(false);
            if (!getGuildMemberResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(getGuildMemberResult);

            if (getGuildMemberResult.Entity is null)
                return new Exception("Member not found");

            if (getGuildResult.Entity.OwnerID == getGuildMemberResult.Entity.User.Value.ID)
                return new DiscordPermissionSet(Enum.GetValues<DiscordPermission>());

            // Get the relevant guild roles
            IReadOnlyList<IRole> guildRoles = getGuildResult.Entity.Roles;
            IRole? everyoneRole = guildRoles.FirstOrDefault(r => r.ID == guildId);
            if (everyoneRole is null)
                return new Exception("No @everyone role found.");

            // Get every complete role object of the member
            List<IRole> guildMemberRoles = getGuildResult.Entity.Roles.Where(r => getGuildMemberResult.Entity.Roles.Contains(r.ID)).ToList();

            // Compute the final permissions
            if (channel.PermissionOverwrites.HasValue)
                return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userId, everyoneRole, guildMemberRoles, channel.PermissionOverwrites.Value));
            else
                return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userId, everyoneRole, guildMemberRoles));
        }
    }
}
