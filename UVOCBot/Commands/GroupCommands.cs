using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Commands.Utilities;
using UVOCBot.Core.Dto;
using UVOCBot.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("group")]
    [Description("Commands that allow groups of members to be created")]
    [RequireContext(ChannelContext.Guild)]
    public class GroupCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly IReplyService _responder;
        private readonly IDbApiService _dbAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public GroupCommands(ICommandContext context, IReplyService responder, IDbApiService dbAPI, IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _responder = responder;
            _dbAPI = dbAPI;
            _guildAPI = guildAPI;
        }

        [Command("list")]
        [Description("Gets all of the groups in this guild")]
        [Ephemeral]
        public async Task<IResult> ListGroupsCommandAsync()
        {
            Result<List<MemberGroupDto>> groups = await _dbAPI.ListGuildMemberGroupsAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!groups.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return groups;
            }

            StringBuilder sb = new();
            sb.Append("Showing ").Append(Formatter.InlineQuote(groups.Entity.Count.ToString())).AppendLine(" groups.").AppendLine();

            foreach (MemberGroupDto g in groups.Entity)
            {
                sb.Append("• ").Append(Formatter.InlineQuote(g.GroupName))
                    .Append(" (").Append(g.UserIds.Count).Append(" members) - created by ")
                    .Append(Formatter.UserMention(g.CreatorId))
                    .Append(", expiring in ")
                    .AppendLine((g.CreatedAt.AddHours(MemberGroupDto.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"));
            }

            return await _responder.RespondWithSuccessAsync(sb.ToString(), CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Gets information about a group")]
        [Ephemeral]
        public async Task<IResult> GetGroupCommandAsync([Description("The name of the group to retrieve")] string groupName)
        {
            Result<MemberGroupDto> groupResult = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!groupResult.IsSuccess)
                return groupResult;

            MemberGroupDto group = groupResult.Entity;

            StringBuilder sb = new();
            sb.Append("Group: ").AppendLine(Formatter.InlineQuote(group.GroupName))
                .Append(group.UserIds.Count).AppendLine(" members")
                .Append("Created by ").AppendLine(Formatter.UserMention(group.CreatorId))
                .Append("Expiring in ").AppendLine((group.CreatedAt.AddHours(MemberGroupDto.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"))
                .AppendLine()
                .AppendLine(Formatter.Bold("Members"));

            foreach (ulong userID in group.UserIds)
            {
                sb.Append(Formatter.UserMention(userID));
                sb.Append(' ');
            }

            return await _responder.RespondWithSuccessAsync(sb.ToString(), CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("create")]
        [Description("Creates a new group from the given members")]
        public async Task<IResult> CreateGroupCommandAsync(
            [Description("The name of the group")] string groupName,
            [Description("The members to include in the group")] string members)
        {
            if (string.IsNullOrEmpty(groupName) || groupName.Length < 3)
                return await _responder.RespondWithUserErrorAsync("The group name must be at least three characters in length.", CancellationToken).ConfigureAwait(false);

            List<ulong> users = ParseUsers(members);

            if (users.Count > 25 || users.Count < 2)
                return await _responder.RespondWithUserErrorAsync("A group must have between 2 and 25 members", CancellationToken).ConfigureAwait(false);

            MemberGroupDto group = new(groupName, _context.GuildID.Value.Value, _context.User.ID.Value, users);

            Result<MemberGroupDto> groupCreationResult = await _dbAPI.CreateMemberGroupAsync(group, CancellationToken).ConfigureAwait(false);
            if (!groupCreationResult.IsSuccess)
            {
                if (groupCreationResult.Error is HttpStatusCodeError er && er.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return await _responder.RespondWithUserErrorAsync(
                        "A group with this name already exists. Please try again with a different name.",
                        CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return Result<IMessage>.FromError(groupCreationResult);
                }
            }

            return await _responder.RespondWithSuccessAsync(
                $"The group {Formatter.Bold(groupName)} has been created with {Formatter.Bold(users.Count.ToString())} members.",
                ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("delete")]
        [Description("Deletes a group")]
        public async Task<IResult> DeleteGroupCommandAsync([Description("The name of the group")] string groupName)
        {
            Result<MemberGroupDto> group = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!group.IsSuccess)
                return group;

            if (_context.User.ID.Value != group.Entity.CreatorId)
            {
                Result<IGuildMember> sender = await _guildAPI.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID, CancellationToken).ConfigureAwait(false);
                if (!sender.IsSuccess || !sender.Entity.Permissions.HasValue)
                    return await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);

                IDiscordPermissionSet senderPerms = sender.Entity.Permissions.Value;

                if (!senderPerms.HasAdminOrPermission(DiscordPermission.ManageGuild) || !senderPerms.HasAdminOrPermission(DiscordPermission.ManageRoles))
                {
                    return await _responder.RespondWithUserErrorAsync(
                        "You must either be the group owner, or have guild/role management permissions, to remove a group.",
                        CancellationToken).ConfigureAwait(false);
                }
            }

            Result groupDeletionResult = await _dbAPI.DeleteMemberGroupAsync(group.Entity.Id).ConfigureAwait(false);
            if (!groupDeletionResult.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return groupDeletionResult;
            }

            return await _responder.RespondWithSuccessAsync($"The group {group.Entity.GroupName} was successfully deleted.", CancellationToken).ConfigureAwait(false);
        }

        private async Task<Result<MemberGroupDto>> GetGroupAsync(string groupName)
        {
            Result<MemberGroupDto> group = await _dbAPI.GetMemberGroupAsync(_context.GuildID.Value.Value, groupName, CancellationToken).ConfigureAwait(false);

            if (!group.IsSuccess)
            {
                if (group.Error is HttpStatusCodeError er && er.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _responder.RespondWithUserErrorAsync("That group does not exist.", CancellationToken).ConfigureAwait(false);
                    return group;
                }
                else
                {
                    await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return group;
                }
            }

            return group;
        }

        private static List<ulong> ParseUsers(string users)
        {
            List<ulong> userIDs = new();

            foreach (string element in users.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string cleaned = element.Trim('<', '@', '!');
                if (ulong.TryParse(cleaned, out ulong id) && !userIDs.Contains(id))
                    userIDs.Add(id);
            }

            return userIDs;
        }
    }
}
