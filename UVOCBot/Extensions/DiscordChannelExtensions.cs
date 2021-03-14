using DSharpPlus.CommandsNext;
using DSharpPlus.Exceptions;
using System.Threading.Tasks;

namespace DSharpPlus.Entities
{
    public static class DiscordChannelExtensions
    {
        #region TrySendDirectMessage

        public static async Task<bool> TrySendDirectMessage(this DiscordChannel dmChannel, string message)
        {
            try
            {
                await dmChannel.SendMessageAsync(message).ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException)
            {
                return false;
            }
        }

        public static async Task<bool> TrySendDirectMessage(this DiscordChannel dmChannel, DiscordMessageBuilder messageBuilder)
        {
            try
            {
                await dmChannel.SendMessageAsync(messageBuilder).ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException)
            {
                return false;
            }
        }

        public static async Task<bool> TrySendDirectMessage(this DiscordChannel dmChannel, DiscordEmbed embed)
        {
            try
            {
                await dmChannel.SendMessageAsync(embed).ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException)
            {
                return false;
            }
        }

        public static async Task<bool> TrySendDirectMessage(this DiscordChannel dmChannel, string message, CommandContext ctx, string failureMessage = null)
        {
            try
            {
                await dmChannel.SendMessageAsync(message).ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException)
            {
                await ctx.RespondWithDMFailureMessage(failureMessage).ConfigureAwait(false);
                return false;
            }
        }

        public static async Task<bool> TrySendDirectMessage(this DiscordChannel dmChannel, DiscordMessageBuilder messageBuilder, CommandContext ctx, string failureMessage = null)
        {
            try
            {
                await dmChannel.SendMessageAsync(messageBuilder).ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException)
            {
                await ctx.RespondWithDMFailureMessage(failureMessage).ConfigureAwait(false);
                return false;
            }
        }

        public static async Task<bool> TrySendDirectMessage(this DiscordChannel dmChannel, DiscordEmbed embed, CommandContext ctx, string failureMessage = null)
        {
            try
            {
                await dmChannel.SendMessageAsync(embed).ConfigureAwait(false);
                return true;
            }
            catch (UnauthorizedException)
            {
                await ctx.RespondWithDMFailureMessage(failureMessage).ConfigureAwait(false);
                return false;
            }
        }

        #endregion

        public static async Task<DiscordMessage> GetMessageAsync(this DiscordChannel channel, CommandContext ctx, ulong messageId)
        {
            try
            {
                return await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                await ctx.RespondAsync("Could not get the provided message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was posted in").ConfigureAwait(false);
                return null;
            }
        }

        /// <summary>
        /// Checks that all the specified members have certain permissions within this channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="permissions"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        public static bool MemberHasPermissions(this DiscordChannel channel, Permissions permissions, params DiscordMember[] members)
        {
            foreach (DiscordMember member in members)
            {
                if ((channel.PermissionsFor(member) & permissions) == 0)
                    return false;
            }

            return true;
        }
    }
}
