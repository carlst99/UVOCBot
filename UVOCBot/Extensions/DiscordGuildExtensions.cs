using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Extensions;

namespace DSharpPlus.Entities
{
    public static class DiscordGuildExtensions
    {
        /// <summary>
        /// Attempts to get a guild channel. If it cannot be found, a suitable fallback channel will instead be chosen
        /// </summary>
        /// <param name="guild">The guild that contains the specified channel</param>
        /// <param name="channelId">The ID of the channel to obtain</param>
        /// <returns></returns>
        public static ChannelReturnedInfo TryGetChannel(this DiscordGuild guild, ulong? channelId)
        {
            if (channelId is not null && guild.Channels.ContainsKey((ulong)channelId))
                return new ChannelReturnedInfo(guild.Channels[(ulong)channelId], ChannelReturnedInfo.GetChannelStatus.Success);
            else if (guild.SystemChannel is not null)
                return new ChannelReturnedInfo(guild.SystemChannel, ChannelReturnedInfo.GetChannelStatus.Fallback);
            else if (guild.GetDefaultChannel() is not null)
                return new ChannelReturnedInfo(guild.GetDefaultChannel(), ChannelReturnedInfo.GetChannelStatus.Fallback);
            else
                return new ChannelReturnedInfo(null, ChannelReturnedInfo.GetChannelStatus.Failure);
        }

        public static async Task<MemberReturnedInfo> TryGetMemberAsync(this DiscordGuild guild, ulong memberId)
        {
            // Try and pull the member from cache in the first instance
            if (guild.Members.ContainsKey(memberId))
            {
                return new MemberReturnedInfo(guild.Members[memberId], MemberReturnedInfo.GetMemberStatus.Success);
            }
            else
            {
                try
                {
                    DiscordMember member = await guild.GetMemberAsync(memberId).ConfigureAwait(false);
                    return new MemberReturnedInfo(member, MemberReturnedInfo.GetMemberStatus.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not get guild member");
                    return new MemberReturnedInfo(null, MemberReturnedInfo.GetMemberStatus.Failure);
                }
            }
        }

        /// <summary>
        /// Gets all members of a guild who have the specified role
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static async Task<List<DiscordMember>> GetMembersWithRoleAsync(this DiscordGuild guild, DiscordRole role)
        {
            IReadOnlyCollection<DiscordMember> guildMembers = await guild.GetAllMembersAsync().ConfigureAwait(false);
            List<DiscordMember> roleOwners = new List<DiscordMember>();

            foreach (DiscordMember member in guildMembers)
            {
                if (member.Roles.Contains(role))
                    roleOwners.Add(member);
            }

            return roleOwners;
        }
    }
}
