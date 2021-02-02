using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Utils;

namespace UVOCBot.Commands
{
    [Group("roles")]
    [Aliases("role", "perms", "permissions")]
    [Description("Commands pertinent to role management")]
    [RequireUserPermissions(Permissions.ManageRoles)]
    [RequireGuild]
    public class RolesModule : BaseCommandModule
    {
        [Command("add-by-reaction")]
        [Aliases("abr", "by-reaction")]
        [Description("Gives the specified role to all users who have reacted to a message with a particular emoji")]
        public async Task AddByReactionCommand(
            CommandContext ctx,
            [Description("The channel that the message was sent in")] DiscordChannel channel,
            [Description("The ID of the message")] ulong messageId,
            [Description("The role that should be assigned to each user")] DiscordRole role,
            [Description("The reaction emoji")] DiscordEmoji emoji)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            DiscordMessage message = await GetMessageAsync(ctx, channel, messageId).ConfigureAwait(false);
            if (message is null)
                return;

            int? reactionCount = await CheckForReactionAsync(ctx, message, emoji).ConfigureAwait(false);
            if (reactionCount is null)
                return;

            // Add the role to users who don't have it
            IReadOnlyList<DiscordUser> users = await message.GetReactionsAsync(emoji, (int)reactionCount).ConfigureAwait(false);
            StringBuilder responseBuilder = new StringBuilder("The role **").Append(role.Name).AppendLine("** was granted to the following users:");
            foreach (DiscordUser user in users)
            {
                DiscordMember member = await GrantRoleToUserAsync(ctx, role, user).ConfigureAwait(false);
                if (member is not null)
                    responseBuilder.Append(member.DisplayName).Append(", ");
            }

            await ctx.RespondAsync(responseBuilder.ToString().Trim(',', ' ')).ConfigureAwait(false);
        }

        [Command("remove-from-all")]
        [Aliases("rfa")]
        [Description("Removes a role from all users who have it")]
        public async Task RemoveFromAllCommand(CommandContext ctx, [Description("The role to remove")] DiscordRole role)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            StringBuilder responseBuilder = new StringBuilder("The role **").Append(role.Name).AppendLine("** was revoked from the following users:");
            IReadOnlyCollection<DiscordMember> guildMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);

            foreach (DiscordMember member in guildMembers)
            {
                if (await RevokeRoleFromUserAsync(ctx, role, member).ConfigureAwait(false))
                    responseBuilder.Append(member.DisplayName).Append(", ");
            }

            await ctx.RespondAsync(responseBuilder.ToString().Trim(',', ' ')).ConfigureAwait(false);
        }

        private static async Task<DiscordMessage> GetMessageAsync(CommandContext ctx, DiscordChannel channel, ulong messageId)
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
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get message");
                await ctx.RespondAsync("An error occured. Please try again").ConfigureAwait(false);
                return null;
            }
        }

        /// <summary>
        /// Checks that a message contains the specified reaction
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message">The message to check</param>
        /// <param name="emoji">The emoji that the users reacted with</param>
        /// <returns>The number of users who reacted</returns>
        private static async Task<int?> CheckForReactionAsync(CommandContext ctx, DiscordMessage message, DiscordEmoji emoji)
        {
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
                await ctx.RespondAsync("The provided message didn't have any reactions of that emoji").ConfigureAwait(false);
                return null;
            }
            else
            {
                return userCount;
            }
        }

        /// <summary>
        /// Gets the user as a guild member and assigns them the role
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="role"></param>
        /// <param name="user"></param>
        /// <returns>The member, if the role was successfully assigned</returns>
        private static async Task<DiscordMember> GrantRoleToUserAsync(CommandContext ctx, DiscordRole role, DiscordUser user)
        {
            MemberReturnedInfo memberInfo = await DiscordClientUtils.TryGetGuildMemberAsync(ctx.Guild, user.Id).ConfigureAwait(false);
            if (memberInfo.Status == MemberReturnedInfo.GetMemberStatus.Failure)
                return null;

            if (!memberInfo.Member.Roles.Contains(role))
            {
                try
                {
                    await memberInfo.Member.GrantRoleAsync(role, $"Role grant requested by {ctx.User.Username}").ConfigureAwait(false);
                    return memberInfo.Member;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cannot grant role to member");
                    return null;
                }
            }
            return null;
        }

        private static async Task<bool> RevokeRoleFromUserAsync(CommandContext ctx, DiscordRole role, DiscordMember member)
        {
            if (member.Roles.Contains(role))
            {
                try
                {
                    await member.RevokeRoleAsync(role, $"Role revokation requested by {ctx.User.Username}").ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cannot revoke role from member");
                    return false;
                }
            }
            return false;
        }
    }
}
