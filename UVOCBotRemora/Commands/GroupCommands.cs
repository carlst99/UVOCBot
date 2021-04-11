using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBotRemora.Services;

namespace UVOCBotRemora.Commands
{
    [Group("group")]
    [Description("Commands that allow groups of members to be created")]
    [RequireContext(ChannelContext.Guild)]
    public class GroupCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IAPIService _dbAPI;

        public GroupCommands(ICommandContext context, MessageResponseHelpers responder, IAPIService dbAPI)
        {
            _context = context;
            _responder = responder;
            _dbAPI = dbAPI;
        }

        [Command("list")]
        [Description("Gets all of the groups in this guild")]
        public async Task<IResult> ListGroupsCommandAsync()
        {
            List<MemberGroupDTO> groups = await _dbAPI.GetAllGuildMemberGroups(_context.GuildID.Value.Value).ConfigureAwait(false);

            StringBuilder sb = new();
            sb.Append("Showing ").Append(Formatter.InlineQuote(groups.Count.ToString())).AppendLine(" groups.").AppendLine();

            foreach (MemberGroupDTO g in groups)
            {
                sb.Append("• ").Append(Formatter.InlineQuote(g.GroupName))
                    .Append(" (").Append(g.UserIds.Count).Append(" members) - created by ")
                    .Append(Formatter.UserMention(g.CreatorId))
                    .Append(", expiring in ")
                    .AppendLine((g.CreatedAt.AddHours(MemberGroupDTO.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"));
            }

            return await _responder.RespondWithSuccessAsync(_context, sb.ToString(), new AllowedMentions(), CancellationToken).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Gets information about a group")]
        public async Task<IResult> GetGroupCommandAsync([Description("The name of the group to retrieve")][DiscordTypeHint(TypeHint.String)] string groupName)
        {
            Result<MemberGroupDTO> groupResult = await GetGroup(groupName).ConfigureAwait(false);
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

            return await _responder.RespondWithSuccessAsync(_context, sb.ToString(), new AllowedMentions(), ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("create")]
        [Description("Creates a new group from the given members")]
        public async Task<IResult> CreateGroupCommandAsync(
            [Description("The name of the group")][DiscordTypeHint(TypeHint.String)] string groupName,
            [Description("The members to include in the group")][DiscordTypeHint(TypeHint.String)] string members)
        {
            if (string.IsNullOrEmpty(groupName) || groupName.Length < 3)
                return await _responder.RespondWithErrorAsync(_context, "The group name must be at least three characters in length.", ct: CancellationToken).ConfigureAwait(false);

            List<ulong> users = ParseUsers(members);

            if (users.Count > 25 || users.Count < 2)
                return await _responder.RespondWithErrorAsync(_context, "A group must have between 2 and 25 members", ct: CancellationToken).ConfigureAwait(false);

            MemberGroupDTO group = new(groupName, _context.GuildID.Value.Value, _context.User.ID.Value, users);

            try
            {
                await _dbAPI.CreateMemberGroup(group).ConfigureAwait(false);
                return await _responder.RespondWithSuccessAsync(_context, $"The group {Formatter.Bold(groupName)} has been created with {Formatter.Bold(users.Count.ToString())} members.", ct: CancellationToken).ConfigureAwait(false);
            }
            catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return await _responder.RespondWithErrorAsync(_context, "A group with this name already exists. Please try again with a different name.", ct: CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _responder.RespondWithErrorAsync(_context, "An error occured. Please try again!", ct: CancellationToken).ConfigureAwait(false);
                return Result<IMessage>.FromError(ex);
            }
        }

        [Command("delete")]
        [Description("Deletes a group")]
        public async Task<IResult> DeleteGroupCommandAsync([Description("The name of the group")][DiscordTypeHint(TypeHint.String)] string groupName)
        {
            Result<MemberGroupDTO> group = await GetGroup(groupName).ConfigureAwait(false);
            if (!group.IsSuccess)
                return group;

            if (_context.User.ID.Value != group.Entity.CreatorId)
            {
                if (_context is InteractionContext ictx && ictx.Member.HasValue && ictx.Member.Value.Permissions.HasValue)
                {
                    IDiscordPermissionSet senderPerms = ictx.Member.Value.Permissions.Value;
                    if (!senderPerms.HasPermission(DiscordPermission.Administrator) || !senderPerms.HasPermission(DiscordPermission.ManageGuild) || !senderPerms.HasPermission(DiscordPermission.ManageRoles))
                        return await _responder.RespondWithErrorAsync(_context, "You must either be the group owner, or have guild/role management permissions, to remove a group.").ConfigureAwait(false);
                }
                else
                {
                    return await _responder.RespondWithErrorAsync(_context, "If you don't own this group, you can only remove it by using the slash command.", ct: CancellationToken).ConfigureAwait(false);
                }
            }

            await _dbAPI.DeleteMemberGroup(group.Entity.Id).ConfigureAwait(false);
            return await _responder.RespondWithSuccessAsync(_context, $"The group {group.Entity.GroupName} was successfully deleted.").ConfigureAwait(false);
        }

        private async Task<Result<MemberGroupDTO>> GetGroup(string groupName)
        {
            try
            {
                return await _dbAPI.GetMemberGroup(_context.GuildID.Value.Value, groupName).ConfigureAwait(false);
            }
            catch (Refit.ValidationApiException va) when (va.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await _responder.RespondWithErrorAsync(_context, "That group does not exist.", ct: CancellationToken).ConfigureAwait(false);
                return Result<MemberGroupDTO>.FromError<Refit.ValidationApiException>(va);
            }
            catch (Exception ex)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", ct: CancellationToken).ConfigureAwait(false);
                return Result<MemberGroupDTO>.FromError(ex);
            }
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
