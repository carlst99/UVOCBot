using DSharpPlus.Entities;
using System.Threading.Tasks;
using UVOCBot.Extensions;

namespace DSharpPlus
{
    public static class DiscordClientExtensions
    {
        /// <summary>
        /// Attempts to get a guild channel. If it cannot be found, a suitable fallback channel will instead be chosen
        /// </summary>
        /// <param name="client">The discord client associated with the guild</param>
        /// <param name="guildId">The ID of the guild in which the channel belongs</param>
        /// <param name="channelId">The ID of the channel to obtain</param>
        /// <returns></returns>
        public static ChannelReturnedInfo TryGetGuildChannel(this DiscordClient client, ulong guildId, ulong? channelId)
        {
            // Try and get the guild to send messages in
            DiscordGuild guild;
            if (client.Guilds.ContainsKey(guildId))
                guild = client.Guilds[guildId];
            else
                return new ChannelReturnedInfo(null, ChannelReturnedInfo.GetChannelStatus.GuildNotFound);

            return guild.TryGetChannel(channelId);
        }

        public static async Task<MemberReturnedInfo> TryGetGuildMemberAsync(this DiscordClient client, ulong guildId, ulong memberId)
        {
            // Try and get the guild to send messages in
            DiscordGuild guild;
            if (client.Guilds.ContainsKey(guildId))
                guild = client.Guilds[guildId];
            else
                return new MemberReturnedInfo(null, MemberReturnedInfo.GetMemberStatus.GuildNotFound);

            return await guild.TryGetMemberAsync(memberId).ConfigureAwait(false);
        }
    }
}
