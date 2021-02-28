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
                await RespondWithDMFailureMessage(ctx, failureMessage).ConfigureAwait(false);
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
                await RespondWithDMFailureMessage(ctx, failureMessage).ConfigureAwait(false);
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
                await RespondWithDMFailureMessage(ctx, failureMessage).ConfigureAwait(false);
                return false;
            }
        }

        public static async Task RespondWithDMFailureMessage(this CommandContext ctx, string failureMessage = null)
        {
            if (string.IsNullOrEmpty(failureMessage))
            {
                await ctx.RespondAsync($"{ctx.Member.Mention} I need to send you a direct message, but you've either disabled them or blocked me." +
                " Please either, **unblock me** or adjust your *Privacy & Safety* settings to **Allow direct messages from server members**." +
                " You can do this for every server (general settings) or just this one (right-click on the icon -> Privacy Settings).").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync(failureMessage).ConfigureAwait(false);
            }
        }

        #endregion

        public  static async Task<DiscordMessage> GetMessageAsync(this DiscordChannel channel, CommandContext ctx, ulong messageId)
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
    }
}
