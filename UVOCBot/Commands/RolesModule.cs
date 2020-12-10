using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Extensions;

namespace UVOCBot.Commands
{
    [Group("roles")]
    [Aliases("role", "r")]
    [Description("Commands pertinent to role management")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public class RolesModule : BaseCommandModule
    {
        [Command("add-by-reaction")]
        [Aliases("abr", "by-reaction")]
        [Description("Gives the specified emoji to all users who have reacted to a message with a particular emoji")]
        public async Task AddByReactionCommand(
            CommandContext ctx,
            [Description("The channel that the message was sent in")] DiscordChannel channel,
            [Description("The ID of the message")] ulong messageId,
            [Description("The role that should be assigned to each user")] DiscordRole role,
            [Description("The reaction emoji")] DiscordEmoji emoji)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Attempt to find the message
            DiscordMessage message;
            try
            {
                message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                await ctx.RespondAsync("Could not get the provided message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was posted in").ConfigureAwait(false);
                return;
            }

            // Check that the message has the specified reaction emoji
            bool reactionFound = false;
            int userCount = 0;
            foreach (DiscordReaction reaction in message.Reactions)
            {
                if (reaction.Emoji.Equals(emoji))
                {
                    reactionFound = true;
                    userCount = reaction.Count;
                    break;
                }
            }
            if (!reactionFound)
            {
                await ctx.RespondAsync("The provided message didn't have any reactions with that emoji").ConfigureAwait(false);
                return;
            }

            // Add the role to users who don't have it
            IReadOnlyList<DiscordUser> users = await message.GetReactionsAsync(emoji, userCount).ConfigureAwait(false);
            StringBuilder responseBuilder = new StringBuilder("The role **").Append(role.Name).AppendLine("** was granted to the following users:");
            foreach (DiscordUser user in users)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
                if (!member.Roles.Contains(role) && await GrantRoleToUserAsync(ctx, role, user).ConfigureAwait(false))
                    responseBuilder.Append(member.GetFriendlyName()).Append(", ");
            }

            await ctx.RespondAsync(responseBuilder.ToString().Trim(',', ' ')).ConfigureAwait(false);
        }

        private static async Task<bool> GrantRoleToUserAsync(CommandContext ctx, DiscordRole role, DiscordUser user)
        {
            DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
            if (!member.Roles.Contains(role))
            {
                await member.GrantRoleAsync(role, $"Role grant requested by {ctx.User.Username}").ConfigureAwait(false);
                return true;
            }
            return false;
        }
    }
}
