using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Group("role")]
    [Description("Commands that help with role management")]
    [RequireContext(ChannelContext.Guild)]
    [RequireUserGuildPermission(DiscordPermission.ManageRoles)]
    public class RoleCommands : CommandGroup
    {
        /// <summary>
        /// The maximum number of usernames that we put into a message embed.
        /// This amount is limited by both Discord embed limitations and our desire to keep messages looking clean
        /// </summary>
        private const int MAX_USERNAMES_IN_LIST = 50;

        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public RoleCommands(ICommandContext context, MessageResponseHelpers responder, IDiscordRestChannelAPI channelAPI, IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _responder = responder;
            _channelAPI = channelAPI;
            _guildAPI = guildAPI;
        }

        [Command("add-by-reaction")]
        [Description("Adds a role to all users who have reacted to a message")]
        public async Task<IResult> AddByReactionCommandAsync(
            [Description("The channel that the message was sent in")][DiscordTypeHint(TypeHint.Channel)] IChannel channel,
            [Description("The numeric ID of the message")] string messageID,
            [Description("The reaction emoji")][DiscordTypeHint(TypeHint.String)] string emoji,
            [Description("The role that should be assigned to each user")][DiscordTypeHint(TypeHint.Role)] IRole role)
        {
            // TODO: Verify that we can assign the role
            Result<IMessage> messageResult = await GetMessage(channel.ID, messageID).ConfigureAwait(false);
            if (!messageResult.IsSuccess)
                return Result.FromError(messageResult);

            // Check that the provided reaction exists
#pragma warning disable CS8604 // Possible null reference argument.
            if (!messageResult.Entity.Reactions.Value.Any(r => r.Emoji.Name.Equals(emoji)))
#pragma warning restore CS8604 // Possible null reference argument.
                return await _responder.RespondWithErrorAsync(_context, $"The emoji {emoji} doesn't exist as a reaction on that message", ct: CancellationToken).ConfigureAwait(false);

            StringBuilder userListBuilder = new();
            int userCount = 0;

            // Attempt to get all the users who reacted to the message
            await foreach (Result<IReadOnlyList<IUser>> usersWhoReacted in _channelAPI.GetAllReactorsAsync(channel.ID, messageResult.Entity.ID, emoji).WithCancellation(CancellationToken).ConfigureAwait(false))
            {
                if (!usersWhoReacted.IsSuccess || usersWhoReacted.Entity is null)
                {
                    await _responder.RespondWithErrorAsync(_context, "Could not get the members who have reacted with " + emoji, ct: CancellationToken).ConfigureAwait(false);
                    return Result.FromError(usersWhoReacted);
                }

                foreach (IUser user in usersWhoReacted.Entity)
                {
                    Result roleAddResult = await _guildAPI.AddGuildMemberRoleAsync(_context.GuildID.Value, user.ID, role.ID, CancellationToken).ConfigureAwait(false);
                    if (roleAddResult.IsSuccess)
                    {
                        // Don't flood the stringbuilder with more names than we'll actually ever use when sending the success message
                        if (userCount < MAX_USERNAMES_IN_LIST)
                            userListBuilder.Append(Formatter.UserMention(user.ID)).Append(", ");

                        userCount++;
                    }
                }
            }

            // Include user mentions if less than 50 members had the role applied (to fit within embed limits, and to not look ridiculous).
            // I did the math in April 2021 and we technically could fit 76 usernames within the limits.
            string messageContent = $"The role {Formatter.RoleMention(role.ID)} was granted to  {Formatter.Bold(userCount.ToString())} users. ";
            if (userCount <= MAX_USERNAMES_IN_LIST)
                messageContent += userListBuilder.ToString().TrimEnd(',', ' ') + ".";

            return await _responder.RespondWithSuccessAsync(_context, messageContent, CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("remove-from-all")]
        [Description("Removes a role from all users who have it")]
        public async Task<IResult> RemoveFromAllCommandAsync([Description("The role to remove")] IRole role)
        {
            StringBuilder userListBuilder = new();
            int userCount = 0;

            await foreach (Result<IReadOnlyList<IGuildMember>> users in _guildAPI.GetAllMembersAsync(_context.GuildID.Value, (m) => m.Roles.Contains(role.ID), CancellationToken))
            {
                if (!users.IsSuccess || users.Entity is null)
                {
                    await _responder.RespondWithErrorAsync(_context, "Could not get members with this role. Please try again later", ct: CancellationToken).ConfigureAwait(false);
                    return Result.FromError(users);
                }

                foreach (IGuildMember member in users.Entity)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    Result roleRemoveResult = await _guildAPI.RemoveGuildMemberRoleAsync(_context.GuildID.Value, member.User.Value.ID, role.ID, CancellationToken).ConfigureAwait(false);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    if (roleRemoveResult.IsSuccess)
                    {
                        if (userCount < MAX_USERNAMES_IN_LIST)
                            userListBuilder.Append(Formatter.UserMention(member.User.Value.ID)).Append(", ");

                        userCount++;
                    }
                }
            }

            string messageContent = $"The role {Formatter.RoleMention(role.ID)} was revoked from  {Formatter.Bold(userCount.ToString())} users. ";
            if (userCount <= MAX_USERNAMES_IN_LIST)
                messageContent += userListBuilder.ToString().TrimEnd(',', ' ') + ".";

            return await _responder.RespondWithSuccessAsync(_context, messageContent, CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to get a message. Sends a failure response if appropriate
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="messageID"></param>
        /// <returns></returns>
        private async Task<Result<IMessage>> GetMessage(Snowflake channel, string messageID)
        {
            //Try to parse the message ID
            if (!ulong.TryParse(messageID, out ulong messageIDParsed))
                return await _responder.RespondWithErrorAsync(_context, "Please submit a valid message ID. You can obtain this by right-clicking a message", ct: CancellationToken).ConfigureAwait(false);

            // Attempt to get the message
            Snowflake messageSnowflake = new(messageIDParsed);
            Result<IMessage> messageResult = await _channelAPI.GetChannelMessageAsync(channel, messageSnowflake, CancellationToken).ConfigureAwait(false);
            if (!messageResult.IsSuccess || messageResult.Entity?.Reactions.HasValue != true)
            {
                await _responder.RespondWithErrorAsync(
                    _context,
                    "Could not find the message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was sent in",
                    ct: CancellationToken).ConfigureAwait(false);
            }

            return messageResult;
        }
    }
}
