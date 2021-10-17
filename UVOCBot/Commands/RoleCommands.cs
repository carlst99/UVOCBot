using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("role")]
    [Description("Commands that help with role management")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public class RoleCommands : CommandGroup
    {
        /// <summary>
        /// The maximum number of usernames that we put into a message embed.
        /// This amount is limited by both Discord embed limitations and our desire to keep messages looking clean
        /// </summary>
        private const int MAX_USERNAMES_IN_LIST = 50;

        private readonly ICommandContext _context;
        private readonly IReplyService _responder;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IPermissionChecksService _permissionChecksService;

        public RoleCommands(
            ICommandContext context,
            IReplyService responder,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi,
            IPermissionChecksService permissionsChecksService)
        {
            _context = context;
            _responder = responder;
            _channelApi = channelApi;
            _guildApi = guildApi;
            _permissionChecksService = permissionsChecksService;
        }

        [Command("add-by-reaction")]
        [Description("Adds a role to all users who have reacted to a message")]
        public async Task<IResult> AddByReactionCommandAsync
        (
            [Description("The channel that the message was sent in")] IChannel channel,
            [Description("The numeric ID of the message")] string messageID,
            [Description("The reaction emoji")] string emoji,
            [Description("The role that should be assigned to each user")] IRole role
        )
        {
            Result<IMessage> messageResult = await GetMessage(channel.ID, messageID).ConfigureAwait(false);
            if (!messageResult.IsSuccess)
                return Result.FromError(messageResult);

            // Check that the provided reaction exists
            if (!messageResult.Entity.Reactions.Value.Any(r => r.Emoji.Name.Equals(emoji)))
                return await _responder.RespondWithUserErrorAsync($"The emoji {emoji} doesn't exist as a reaction on that message", ct: CancellationToken).ConfigureAwait(false);

            // Ensure that we can manipulate this role
            IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, new List<ulong> { role.ID.Value }, CancellationToken).ConfigureAwait(false);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            StringBuilder userListBuilder = new();
            int userCount = 0;

            // Attempt to get all the users who reacted to the message
            await foreach (Result<IReadOnlyList<IUser>> usersWhoReacted in _channelApi.GetAllReactorsAsync(channel.ID, messageResult.Entity.ID, emoji).WithCancellation(CancellationToken).ConfigureAwait(false))
            {
                if (!usersWhoReacted.IsSuccess || usersWhoReacted.Entity is null)
                {
                    await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return Result.FromError(usersWhoReacted);
                }

                foreach (IUser user in usersWhoReacted.Entity)
                {
                    Result roleAddResult = await _guildApi.AddGuildMemberRoleAsync(_context.GuildID.Value, user.ID, role.ID, default, CancellationToken).ConfigureAwait(false);
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
            string messageContent = $"The role { Formatter.RoleMention(role.ID) } was granted to  { Formatter.Bold(userCount.ToString()) } users. ";
            if (userCount <= MAX_USERNAMES_IN_LIST)
                messageContent += userListBuilder.ToString().TrimEnd(',', ' ') + ".";

            return await _responder.RespondWithSuccessAsync(messageContent, CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("remove-from-all")]
        [Description("Removes a role from all users who have it")]
        public async Task<IResult> RemoveFromAllCommandAsync([Description("The role to remove")] IRole role)
        {
            StringBuilder userListBuilder = new();
            int userCount = 0;

            // Ensure that we can manipulate this role
            IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, new List<ulong> { role.ID.Value }, CancellationToken).ConfigureAwait(false);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            await foreach (Result<IReadOnlyList<IGuildMember>> users in _guildApi.GetAllMembersAsync(_context.GuildID.Value, (m) => m.Roles.Contains(role.ID), CancellationToken))
            {
                if (!users.IsSuccess || users.Entity is null)
                {
                    await _responder.RespondWithUserErrorAsync("Could not get members with this role. Please try again later", CancellationToken).ConfigureAwait(false);
                    return Result.FromError(users);
                }

                foreach (IGuildMember member in users.Entity)
                {
                    Result roleRemoveResult = await _guildApi.RemoveGuildMemberRoleAsync(_context.GuildID.Value, member.User.Value.ID, role.ID, default, CancellationToken).ConfigureAwait(false);
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

            return await _responder.RespondWithSuccessAsync(messageContent, CancellationToken, new AllowedMentions()).ConfigureAwait(false);
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
                return await _responder.RespondWithUserErrorAsync("Please submit a valid message ID. You can obtain this by right-clicking a message", ct: CancellationToken).ConfigureAwait(false);

            // Attempt to get the message
            Snowflake messageSnowflake = new(messageIDParsed);
            Result<IMessage> messageResult = await _channelApi.GetChannelMessageAsync(channel, messageSnowflake, CancellationToken).ConfigureAwait(false);
            if (!messageResult.IsSuccess || messageResult.Entity?.Reactions.HasValue != true)
            {
                await _responder.RespondWithUserErrorAsync(
                    "Could not find the message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was sent in",
                    ct: CancellationToken).ConfigureAwait(false);
            }

            return messageResult;
        }
    }
}
