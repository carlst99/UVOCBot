using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Plugins.Roles.Commands;

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
    private const int MAX_USERNAMES_IN_LIST = 100;

    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly FeedbackService _feedbackService;

    public RoleCommands
    (
        ICommandContext context,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        IPermissionChecksService permissionsChecksService,
        FeedbackService feedbackService
    )
    {
        _context = context;
        _channelApi = channelApi;
        _guildApi = guildApi;
        _permissionChecksService = permissionsChecksService;
        _feedbackService = feedbackService;
    }

    [Command("add-by-reaction")]
    [Description("Adds a role to all users who have reacted to a message")]
    public async Task<IResult> AddByReactionCommandAsync
    (
        [Description("The channel that the message was sent in")][ChannelTypes(ChannelType.GuildText)] IChannel channel,
        [Description("The numeric ID of the message")] Snowflake messageID,
        [Description("The reaction emoji")] string emoji,
        [Description("The role that should be assigned to each user")] IRole role
    )
    {
        Result<IReadOnlyList<IReaction>> getMessageReactions = await GetMessageReactionsAsync(channel.ID, messageID);
        if (!getMessageReactions.IsDefined(out IReadOnlyList<IReaction>? messageReactions))
            return getMessageReactions;

        // Check that the provided reaction exists
        if (!messageReactions.Any(r => r.Emoji.Name.Equals(emoji)))
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                $"The emoji {emoji} doesn't exist as a reaction on that message",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        // Ensure that we can manipulate this role
        IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles
        (
            _context.GuildID.Value,
            new List<ulong> { role.ID.Value },
            CancellationToken
        ).ConfigureAwait(false);

        if (!canManipulateRoles.IsSuccess)
            return canManipulateRoles;

        StringBuilder userListBuilder = new();
        int userCount = 0;

        // Attempt to get all the users who reacted to the message
        await foreach (Result<IReadOnlyList<IUser>> usersWhoReacted in _channelApi.GetAllReactorsAsync(channel.ID, messageID, emoji).WithCancellation(CancellationToken).ConfigureAwait(false))
        {
            if (!usersWhoReacted.IsDefined())
                return usersWhoReacted;

            foreach (IUser user in usersWhoReacted.Entity)
            {
                Result roleAddResult = await _guildApi.AddGuildMemberRoleAsync(_context.GuildID.Value, user.ID, role.ID, default, CancellationToken).ConfigureAwait(false);
                if (!roleAddResult.IsSuccess)
                    continue;

                // Don't flood the StringBuilder with more names than we'll actually ever use when sending the success message
                if (userCount < MAX_USERNAMES_IN_LIST)
                    userListBuilder.Append(Formatter.UserMention(user.ID)).Append(", ");

                userCount++;
            }
        }

        // Include user mentions if less than 100 members had the role applied (to fit within embed limits, and to not look ridiculous).
        // I did the math in April 2021 and we technically could fit 76 usernames within the limits.
        string messageContent = $"The role {Formatter.RoleMention(role.ID)} was granted to {Formatter.Bold(userCount.ToString())} users, including: ";
        messageContent += userListBuilder.ToString().TrimEnd(',', ' ') + ".";

        return await _feedbackService.SendContextualSuccessAsync
        (
            messageContent,
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions()),
            ct: CancellationToken
        ).ConfigureAwait(false);
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
            if (!users.IsDefined())
                return users;

            foreach (IGuildMember member in users.Entity)
            {
                Result roleRemoveResult = await _guildApi.RemoveGuildMemberRoleAsync(_context.GuildID.Value, member.User.Value.ID, role.ID, default, CancellationToken).ConfigureAwait(false);
                if (!roleRemoveResult.IsSuccess)
                    continue;

                if (userCount < MAX_USERNAMES_IN_LIST)
                    userListBuilder.Append(Formatter.UserMention(member.User.Value.ID)).Append(", ");

                userCount++;
            }
        }

        string messageContent = $"The role {Formatter.RoleMention(role.ID)} was revoked from  {Formatter.Bold(userCount.ToString())} users. ";
        messageContent += userListBuilder.ToString().TrimEnd(',', ' ') + ".";

        return await _feedbackService.SendContextualSuccessAsync
        (
            messageContent,
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions()),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Tries to retrieve the reactions on a message. Sends a failure response if appropriate
    /// </summary>
    /// <param name="channelID">The ID of the channel in which the message resides.</param>
    /// <param name="messageID">The ID of the message to retrieve.</param>
    /// <returns>The reactions.</returns>
    private async Task<Result<IReadOnlyList<IReaction>>> GetMessageReactionsAsync
    (
        Snowflake channelID,
        Snowflake messageID
    )
    {
        Result<IMessage> messageResult = await _channelApi.GetChannelMessageAsync(channelID, messageID, CancellationToken).ConfigureAwait(false);
        if (!messageResult.IsDefined(out IMessage? message))
        {
            return new GenericCommandError
            (
                "Could not find the message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was sent in"
            );
        }

        return !message.Reactions.IsDefined(out IReadOnlyList<IReaction>? reactions)
            ? new GenericCommandError("That message appears to have no reactions")
            : Result<IReadOnlyList<IReaction>>.FromSuccess(reactions);
    }
}
