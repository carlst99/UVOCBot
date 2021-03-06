﻿using Remora.Commands.Attributes;
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
using UVOCBot.Core.Model;
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
        private readonly MessageResponseHelpers _responder;
        private readonly IDbApiService _dbAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public GroupCommands(ICommandContext context, MessageResponseHelpers responder, IDbApiService dbAPI, IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _responder = responder;
            _dbAPI = dbAPI;
            _guildAPI = guildAPI;
        }

        [Command("list")]
        [Description("Gets all of the groups in this guild")]
        public async Task<IResult> ListGroupsCommandAsync()
        {
            Result<List<MemberGroupDTO>> groups = await _dbAPI.ListGuildMemberGroupsAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!groups.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again", CancellationToken).ConfigureAwait(false);
                return groups;
            }

            StringBuilder sb = new();
            sb.Append("Showing ").Append(Formatter.InlineQuote(groups.Entity.Count.ToString())).AppendLine(" groups.").AppendLine();

            foreach (MemberGroupDTO g in groups.Entity)
            {
                sb.Append("• ").Append(Formatter.InlineQuote(g.GroupName))
                    .Append(" (").Append(g.UserIds.Count).Append(" members) - created by ")
                    .Append(Formatter.UserMention(g.CreatorId))
                    .Append(", expiring in ")
                    .AppendLine((g.CreatedAt.AddHours(MemberGroupDTO.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"));
            }

            return await _responder.RespondWithSuccessAsync(_context, sb.ToString(), CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Gets information about a group")]
        public async Task<IResult> GetGroupCommandAsync([Description("The name of the group to retrieve")] string groupName)
        {
            Result<MemberGroupDTO> groupResult = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!groupResult.IsSuccess)
                return groupResult;

            MemberGroupDTO group = groupResult.Entity;

            StringBuilder sb = new();
            sb.Append("Group: ").AppendLine(Formatter.InlineQuote(group.GroupName))
                .Append(group.UserIds.Count).AppendLine(" members")
                .Append("Created by ").AppendLine(Formatter.UserMention(group.CreatorId))
                .Append("Expiring in ").AppendLine((group.CreatedAt.AddHours(MemberGroupDTO.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"))
                .AppendLine()
                .AppendLine(Formatter.Bold("Members"));

            foreach (ulong userID in group.UserIds)
            {
                sb.Append(Formatter.UserMention(userID));
                sb.Append(' ');
            }

            return await _responder.RespondWithSuccessAsync(_context, sb.ToString(), CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("create")]
        [Description("Creates a new group from the given members")]
        public async Task<IResult> CreateGroupCommandAsync(
            [Description("The name of the group")] string groupName,
            [Description("The members to include in the group")] string members)
        {
            if (string.IsNullOrEmpty(groupName) || groupName.Length < 3)
                return await _responder.RespondWithErrorAsync(_context, "The group name must be at least three characters in length.", ct: CancellationToken).ConfigureAwait(false);

            List<ulong> users = ParseUsers(members);

            if (users.Count > 25 || users.Count < 2)
                return await _responder.RespondWithErrorAsync(_context, "A group must have between 2 and 25 members", ct: CancellationToken).ConfigureAwait(false);

            MemberGroupDTO group = new(groupName, _context.GuildID.Value.Value, _context.User.ID.Value, users);

            Result<MemberGroupDTO> groupCreationResult = await _dbAPI.CreateMemberGroupAsync(group, CancellationToken).ConfigureAwait(false);
            if (!groupCreationResult.IsSuccess)
            {
                if (groupCreationResult.Error is HttpStatusCodeError er && er.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return await _responder.RespondWithErrorAsync(
                        _context,
                        "A group with this name already exists. Please try again with a different name.",
                        ct: CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again", CancellationToken).ConfigureAwait(false);
                    return Result<IMessage>.FromError(groupCreationResult);
                }
            }

            return await _responder.RespondWithSuccessAsync(
                _context,
                $"The group {Formatter.Bold(groupName)} has been created with {Formatter.Bold(users.Count.ToString())} members.",
                ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("delete")]
        [Description("Deletes a group")]
        public async Task<IResult> DeleteGroupCommandAsync([Description("The name of the group")] string groupName)
        {
            Result<MemberGroupDTO> group = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!group.IsSuccess)
                return group;

            if (_context.User.ID.Value != group.Entity.CreatorId)
            {
                Result<IGuildMember> sender = await _guildAPI.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID, CancellationToken).ConfigureAwait(false);
                if (!sender.IsSuccess || !sender.Entity.Permissions.HasValue)
                    return await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again later!", CancellationToken).ConfigureAwait(false);

                IDiscordPermissionSet senderPerms = sender.Entity.Permissions.Value;

                if (!senderPerms.HasPermission(DiscordPermission.Administrator) || !senderPerms.HasPermission(DiscordPermission.ManageGuild) || !senderPerms.HasPermission(DiscordPermission.ManageRoles))
                {
                    return await _responder.RespondWithErrorAsync(
                        _context,
                        "You must either be the group owner, or have guild/role management permissions, to remove a group.",
                        CancellationToken).ConfigureAwait(false);
                }
            }

            Result groupDeletionResult = await _dbAPI.DeleteMemberGroupAsync(group.Entity.Id).ConfigureAwait(false);
            if (!groupDeletionResult.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again", CancellationToken).ConfigureAwait(false);
                return groupDeletionResult;
            }

            return await _responder.RespondWithSuccessAsync(_context, $"The group {group.Entity.GroupName} was successfully deleted.", CancellationToken).ConfigureAwait(false);
        }

        private async Task<Result<MemberGroupDTO>> GetGroupAsync(string groupName)
        {
            Result<MemberGroupDTO> group = await _dbAPI.GetMemberGroupAsync(_context.GuildID.Value.Value, groupName, CancellationToken).ConfigureAwait(false);

            if (!group.IsSuccess)
            {
                if (group.Error is HttpStatusCodeError er && er.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _responder.RespondWithErrorAsync(_context, "That group does not exist.", CancellationToken).ConfigureAwait(false);
                    return group;
                }
                else
                {
                    await _responder.RespondWithErrorAsync(_context, "Something went wrong. Please try again", CancellationToken).ConfigureAwait(false);
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
