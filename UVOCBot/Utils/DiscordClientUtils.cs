using DSharpPlus;
using DSharpPlus.Entities;
using Serilog;
using System;
using System.Threading.Tasks;

namespace UVOCBot.Utils
{
    public static class DiscordClientUtils
    {
        /// <summary>
        /// Attempts to get a guild channel. If it cannot be found, a suitable fallback channel will instead be chosen
        /// </summary>
        /// <param name="guild">The guild that contains the specified channel</param>
        /// <param name="channelId">The ID of the channel to obtain</param>
        /// <returns></returns>
        public static ChannelReturnedInfo TryGetGuildChannel(DiscordGuild guild, ulong? channelId)
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

        /// <summary>
        /// Attempts to get a guild channel. If it cannot be found, a suitable fallback channel will instead be chosen
        /// </summary>
        /// <param name="client">The discord client associated with the guild</param>
        /// <param name="guildId">The ID of the guild in which the channel belongs</param>
        /// <param name="channelId">The ID of the channel to obtain</param>
        /// <returns></returns>
        public static ChannelReturnedInfo TryGetGuildChannel(DiscordClient client, ulong guildId, ulong? channelId)
        {
            // Try and get the guild to send messages in
            DiscordGuild guild;
            if (client.Guilds.ContainsKey(guildId))
                guild = client.Guilds[guildId];
            else
                return new ChannelReturnedInfo(null, ChannelReturnedInfo.GetChannelStatus.GuildNotFound);

            return TryGetGuildChannel(guild, channelId);
        }

        public static async Task<MemberReturnedInfo> TryGetGuildMemberAsync(DiscordGuild guild, ulong memberId)
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

        public static async Task<MemberReturnedInfo> TryGetGuildMemberAsync(DiscordClient client, ulong guildId, ulong memberId)
        {
            // Try and get the guild to send messages in
            DiscordGuild guild;
            if (client.Guilds.ContainsKey(guildId))
                guild = client.Guilds[guildId];
            else
                return new MemberReturnedInfo(null, MemberReturnedInfo.GetMemberStatus.GuildNotFound);

            return await TryGetGuildMemberAsync(guild, memberId).ConfigureAwait(false);
        }
    }
}
