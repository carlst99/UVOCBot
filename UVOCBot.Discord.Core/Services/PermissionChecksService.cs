﻿using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Discord.Core.Services;

/// <inheritdoc cref="IPermissionChecksService"/>
public class PermissionChecksService : IPermissionChecksService
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;

    public PermissionChecksService(
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi)
    {
        _channelApi = channelApi;
        _guildApi = guildApi;
    }

    /// <inheritdoc />
    public async Task<Result> CanManipulateRoles(Snowflake guildID, IEnumerable<ulong> roleIds, CancellationToken ct = default)
    {
        List<ulong> roleIDList = roleIds.ToList();
        if (roleIDList.Count == 0)
            return Result.FromSuccess();

        Result<IReadOnlyList<IRole>> getGuildRoles = await _guildApi.GetGuildRolesAsync(guildID, ct).ConfigureAwait(false);
        if (!getGuildRoles.IsSuccess)
            return Result.FromError(getGuildRoles);

        Result<IGuildMember> getCurrentMember = await _guildApi.GetGuildMemberAsync(guildID, DiscordConstants.UserId, ct).ConfigureAwait(false);
        if (!getCurrentMember.IsSuccess)
            return Result.FromError(getCurrentMember);

        // Enumerate roles in order from highest to lowest ranked, so that logically the first role in the current member's role list is its highest
        IRole? highestRole = getGuildRoles.Entity.OrderByDescending(r => r.Position)
            .FirstOrDefault(role => getCurrentMember.Entity.Roles.Contains(role.ID));

        if (highestRole is null)
            return Result.FromError(new RoleManipulationError("I require a role to be able to assign roles to others."));

        // Check that each role is assignable by us
        foreach (ulong roleId in roleIDList)
        {
            if (getGuildRoles.Entity.All(r => r.ID.Value != roleId))
                return Result.FromError(new RoleManipulationError("A given role does not exist."));

            IRole role = getGuildRoles.Entity.First(r => r.ID.Value == roleId);
            if (role.Position > highestRole.Position)
            {
                return Result.FromError
                (
                    new RoleManipulationError($"Cannot assign the { Formatter.RoleMention(role.ID) } role, as it is positioned above my own highest role.")
                );
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
            return new ContextError(ContextError.GuildTextChannels);

        Snowflake guildId = channel.GuildID.Value;

        // Get the guild
        Result<IGuild> getGuildResult = await _guildApi.GetGuildAsync(guildId, ct: ct).ConfigureAwait(false);
        if (!getGuildResult.IsSuccess)
            return Result<IDiscordPermissionSet>.FromError(getGuildResult);

        // Get and check the guild member
        Result<IGuildMember> getGuildMemberResult = await _guildApi.GetGuildMemberAsync(guildId, userId, ct).ConfigureAwait(false);
        if (!getGuildMemberResult.IsDefined(out IGuildMember? guildMember))
            return Result<IDiscordPermissionSet>.FromError(getGuildMemberResult);

        if (getGuildResult.Entity.OwnerID == guildMember.User.Value.ID)
            return new DiscordPermissionSet(Enum.GetValues<DiscordPermission>());

        // Get the relevant guild roles
        IReadOnlyList<IRole> guildRoles = getGuildResult.Entity.Roles;
        IRole? everyoneRole = guildRoles.FirstOrDefault(r => r.ID == guildId);
        if (everyoneRole is null)
            return new Exception("No @everyone role found.");

        // Get every complete role object of the member
        List<IRole> guildMemberRoles = getGuildResult.Entity.Roles.Where(r => guildMember.Roles.Contains(r.ID)).ToList();

        // Compute the final permissions
        if (channel.PermissionOverwrites.HasValue)
            return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userId, everyoneRole, guildMemberRoles, channel.PermissionOverwrites.Value));

        return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userId, everyoneRole, guildMemberRoles));
    }
}
