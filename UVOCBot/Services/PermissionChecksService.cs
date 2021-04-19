using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class PermissionChecksService : IPermissionChecksService
    {
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public PermissionChecksService(
            IDiscordRestChannelAPI channelAPI,
            IDiscordRestGuildAPI guildAPI)
        {
            _channelAPI = channelAPI;
            _guildAPI = guildAPI;
        }

        public async Task<Result<IDiscordPermissionSet>> PermissionsInChannel(Snowflake channelID, Snowflake userID, CancellationToken ct)
        {
            // Get the channel
            Result<IChannel> channelResult = await _channelAPI.GetChannelAsync(channelID, ct).ConfigureAwait(false);
            if (!channelResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(channelResult);
            IChannel channel = channelResult.Entity;

            // Ensure that the channel is part of a guild
            if (!channel.GuildID.HasValue)
                return new ArgumentException("The provided channel is not from a guild.", nameof(channelID));
            Snowflake guildID = channelResult.Entity.GuildID.Value;

            // Get the guild
            Result<IGuild> guildResult = await _guildAPI.GetGuildAsync(guildID, false, ct).ConfigureAwait(false);
            if (!guildResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(guildResult);
            IGuild guild = guildResult.Entity;

            // Get the guild member
            Result<IGuildMember> guildMemberResult = await _guildAPI.GetGuildMemberAsync(guildID, userID, ct).ConfigureAwait(false);
            if (!guildMemberResult.IsSuccess)
                return Result<IDiscordPermissionSet>.FromError(guildMemberResult);
            IGuildMember guildMember = guildMemberResult.Entity;

            // Find the everyone role
            IRole? everyoneRole = guild.Roles.FirstOrDefault(r => r.ID == guildID);
            if (everyoneRole is null)
                return new Exception("No @everyone role found.");

            List<IRole> guildMemberRoles = guild.Roles.Where(r => guildMember.Roles.Contains(r.ID)).ToList();

            // Compute the final permissions
            if (channel.PermissionOverwrites.HasValue)
                return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userID, everyoneRole, guildMemberRoles, channel.PermissionOverwrites.Value));
            else
                return Result<IDiscordPermissionSet>.FromSuccess(DiscordPermissionSet.ComputePermissions(userID, everyoneRole, guildMemberRoles));
        }
    }
}
