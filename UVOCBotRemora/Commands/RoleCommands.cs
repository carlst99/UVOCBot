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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVOCBotRemora.Commands
{
    [Group("role")]
    public class RoleCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly CommandContextReponses _responder;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public RoleCommands(ICommandContext context, CommandContextReponses responder, IDiscordRestChannelAPI channelAPI, IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _responder = responder;
            _channelAPI = channelAPI;
            _guildAPI = guildAPI;
        }

        [Command("add-by-reaction")]
        [Description("Adds a role to all users who have reacted to a message")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<IResult> GetServerPopulationCommandAsync(
            [Description("The channel that the message was sent in")][DiscordTypeHint(TypeHint.Channel)] IChannel channel,
            [Description("The numeric ID of the message")] string messageID,
            [Description("The reaction emoji")][DiscordTypeHint(TypeHint.String)] string emoji,
            [Description("The role that should be assigned to each user")][DiscordTypeHint(TypeHint.Role)] IRole role)
        {
            //Try to parse the message ID
            if (!ulong.TryParse(messageID, out ulong messageIDParsed))
                return await _responder.RespondWithErrorAsync(_context, "Please submit a valid message ID. You can obtain this by right-clicking a message", ct: CancellationToken).ConfigureAwait(false);

            // Attempt to get the method
            Snowflake messageSnowflake = new(messageIDParsed);
            Result<IMessage> messageResult = await _channelAPI.GetChannelMessageAsync(channel.ID, messageSnowflake, CancellationToken).ConfigureAwait(false);
            if (!messageResult.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(
                    _context,
                    "Could not find the message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was sent in",
                    ct: CancellationToken).ConfigureAwait(false);

                return Result.FromError(messageResult);
            }

            // Check that the provided reaction exists
            if (!messageResult.Entity.Reactions.Value.Any(r => r.Emoji.Name.Equals(emoji)))
                return await _responder.RespondWithErrorAsync(_context, $"The emoji {emoji} doesn't exist as a reaction on that message", ct: CancellationToken).ConfigureAwait(false);

            // Attempt to get all the users who reacted to the message
            Result<IReadOnlyList<IUser>> usersWhoReacted = await _channelAPI.GetAllReactionsAsync(channel.ID, messageSnowflake, emoji, ct: CancellationToken).ConfigureAwait(false);
            if (!usersWhoReacted.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Could not get the members who have reacted with " + emoji, ct: CancellationToken).ConfigureAwait(false);
                return Result.FromError(usersWhoReacted);
            }

            // TODO: Verify that we can assign the role

            // Add the role to all users and build a string list of all of them
            StringBuilder userListBuilder = new();
            int userCount = 0;
            foreach (IUser user in usersWhoReacted.Entity)
            {
                Result roleAddResult = await _guildAPI.AddGuildMemberRoleAsync(_context.GuildID.Value, user.ID, role.ID, CancellationToken).ConfigureAwait(false);
                if (roleAddResult.IsSuccess)
                {
                    userListBuilder.Append(Formatter.UserMention(user.ID)).Append(", ");
                    userCount++;
                }
            }

            // Include user mentions if less than 25 members had the role applied, otherwise include number
            string messageContent = $"The role {Formatter.RoleMention(role.ID)} was granted to ";
            string usersContent = usersWhoReacted.Entity.Count <= 25
                ? userListBuilder.ToString()
                : userCount + " users.";

            // TODO: Do math on max username (well, mention string) length and embed.Description max length, and set maximum entity count for listing usernames as so

            return await _responder.RespondWithSuccessAsync(_context, messageContent + usersContent, new AllowedMentions(), CancellationToken).ConfigureAwait(false);
        }
    }
}
