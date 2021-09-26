using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("group")]
    [Description("Commands that allow groups of members to be created")]
    [RequireContext(ChannelContext.Guild)]
    public class GroupCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IReplyService _replyService;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public GroupCommands(
            ICommandContext context,
            DiscordContext dbContext,
            IReplyService replyService,
            IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _dbContext = dbContext;
            _replyService = replyService;
            _guildAPI = guildAPI;
        }

        [Command("list")]
        [Description("Gets all of the groups in this guild")]
        [Ephemeral]
        public async Task<IResult> ListGroupsCommandAsync()
        {
            StringBuilder sb = new();
            sb.AppendLine("Groups:");

            foreach (MemberGroup g in _dbContext.MemberGroups)
            {
                sb.Append("• ").Append(Formatter.InlineQuote(g.GroupName))
                    .Append(" (").Append(g.UserIds.Count).AppendLine(" members).");
            }

            return await _replyService.RespondWithSuccessAsync(sb.ToString(), CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Gets information about a group")]
        [Ephemeral]
        public async Task<IResult> GetGroupCommandAsync([Description("The name of the group to retrieve")] string groupName)
        {
            Result<MemberGroup> getGroupResult = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!getGroupResult.IsSuccess)
                return Result.FromSuccess();

            MemberGroup group = getGroupResult.Entity;

            StringBuilder sb = new();
            sb.Append("Group: ").AppendLine(Formatter.InlineQuote(group.GroupName))
                .Append(group.UserIds.Count).AppendLine(" members")
                .Append("Created by ").AppendLine(Formatter.UserMention(group.CreatorId))
                .Append("Expiring in ").AppendLine((group.CreatedAt.AddHours(MemberGroup.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"))
                .AppendLine()
                .AppendLine(Formatter.Bold("Members"));

            foreach (ulong userID in group.UserIds)
            {
                sb.Append(Formatter.UserMention(userID));
                sb.Append(' ');
            }

            return await _replyService.RespondWithSuccessAsync(sb.ToString(), CancellationToken, new AllowedMentions()).ConfigureAwait(false);
        }

        [Command("create")]
        [Description("Creates a new group from the given members")]
        public async Task<IResult> CreateGroupCommandAsync(
            [Description("The name of the group")] string groupName,
            [Description("The members to include in the group")] string members)
        {
            if (string.IsNullOrEmpty(groupName) || groupName.Length < 3)
                return await _replyService.RespondWithUserErrorAsync("The group name must be at least three characters in length.", CancellationToken).ConfigureAwait(false);

            List<ulong> users = ParseUsers(members);

            if (users.Count > 25 || users.Count < 2)
                return await _replyService.RespondWithUserErrorAsync("A group must have between 2 and 25 members", CancellationToken).ConfigureAwait(false);

            bool groupExists = await _dbContext.MemberGroups.AnyAsync(g => g.GuildId == _context.GuildID.Value.Value && g.GroupName == groupName).ConfigureAwait(false);
            if (groupExists)
                return await _replyService.RespondWithUserErrorAsync("A group with this name already exists. Please try again with a different name.", CancellationToken).ConfigureAwait(false);

            MemberGroup group = new(_context.GuildID.Value.Value, groupName)
            {
                UserIds = users,
                CreatorId = _context.User.ID.Value
            };

            _dbContext.Add(group);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync(
                $"The group {Formatter.Bold(groupName)} has been created with {Formatter.Bold(users.Count.ToString())} members.",
                ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("delete")]
        [Description("Deletes a group")]
        public async Task<IResult> DeleteGroupCommandAsync([Description("The name of the group")] string groupName)
        {
            Result<MemberGroup> group = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!group.IsSuccess)
                return Result.FromSuccess();

            if (_context.User.ID.Value != group.Entity.CreatorId)
            {
                Result<IGuildMember> sender = await _guildAPI.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID, CancellationToken).ConfigureAwait(false);
                if (!sender.IsSuccess || !sender.Entity.Permissions.HasValue)
                    return await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);

                IDiscordPermissionSet senderPerms = sender.Entity.Permissions.Value;

                if (!senderPerms.HasAdminOrPermission(DiscordPermission.ManageGuild) || !senderPerms.HasAdminOrPermission(DiscordPermission.ManageRoles))
                {
                    return await _replyService.RespondWithUserErrorAsync(
                        "You must either be the group owner, or have guild/role management permissions, to remove a group.",
                        CancellationToken).ConfigureAwait(false);
                }
            }

            _dbContext.Remove(group.Entity);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync($"The group {group.Entity.GroupName} was successfully deleted.", CancellationToken).ConfigureAwait(false);
        }

        private async Task<Result<MemberGroup>> GetGroupAsync(string groupName)
        {
            MemberGroup? group = await _dbContext.MemberGroups.FirstOrDefaultAsync(g => g.GuildId == _context.GuildID.Value.Value && g.GroupName == groupName, CancellationToken).ConfigureAwait(false);

            if (group is null)
            {
                await _replyService.RespondWithUserErrorAsync("A group with that name does not exist.", CancellationToken).ConfigureAwait(false);
                return new NotFoundError();
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
