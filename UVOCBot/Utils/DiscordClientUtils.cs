using DSharpPlus;
using DSharpPlus.Entities;

namespace UVOCBot.Utils
{
    public static class DiscordClientUtils
    {
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

            if (channelId is not null && guild.Channels.ContainsKey((ulong)channelId))
                return new ChannelReturnedInfo(guild.Channels[(ulong)channelId], ChannelReturnedInfo.GetChannelStatus.Success);
            else if (guild.SystemChannel is not null)
                return new ChannelReturnedInfo(guild.SystemChannel, ChannelReturnedInfo.GetChannelStatus.Fallback);
            else if (guild.GetDefaultChannel() is not null)
                return new ChannelReturnedInfo(guild.GetDefaultChannel(), ChannelReturnedInfo.GetChannelStatus.Fallback);
            else
                return new ChannelReturnedInfo(null, ChannelReturnedInfo.GetChannelStatus.Failure);
        }
    }
}
