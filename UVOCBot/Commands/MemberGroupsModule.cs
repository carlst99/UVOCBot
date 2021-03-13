using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBot.Extensions;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    [Description("Commands that allow groups of members to be created")]
    [RequireGuild]
    [Group("group")]
    public class MemberGroupsModule : BaseCommandModule
    {
        public IApiService DbApi { get; set; }

        [Command("list")]
        [Description("Gets all of the groups created in this guild")]
        public async Task ListGroupsCommand(CommandContext ctx)
        {
            List<MemberGroupDTO> groups = await DbApi.GetAllGuildMemberGroups(ctx.Guild.Id).ConfigureAwait(false);

            StringBuilder sb = new();
            sb.Append("Showing ").Append(Formatter.InlineCode(groups.Count.ToString())).Append(" groups for ")
                .AppendLine(Formatter.InlineCode(ctx.Guild.Name))
                .AppendLine();

            foreach (MemberGroupDTO g in groups)
            {
                sb.Append("• ").Append(Formatter.InlineCode(g.GroupName)).Append(" (").Append(g.UserIds.Count).Append(" members) - created by ");

                await AppendMember(ctx.Guild, g.CreatorId, sb).ConfigureAwait(false);

                sb.Append(", expiring in ")
                    .AppendLine((g.CreatedAt.AddHours(MemberGroupDTO.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"));
            }

            DiscordMessageBuilder builder = new();
            builder.WithContent(sb.ToString());
            builder.WithAllowedMentions(Mentions.None);

            await ctx.RespondAsync(builder).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Gets information about a group")]
        public async Task GetGroupCommand(CommandContext ctx, [Description("The name of the group to retrieve")] string groupName)
        {
            try
            {
                MemberGroupDTO group = await DbApi.GetMemberGroup(ctx.Guild.Id, groupName).ConfigureAwait(false);

                StringBuilder sb = new();
                sb.Append("Group: ").AppendLine(Formatter.InlineCode(group.GroupName))
                    .Append(group.UserIds.Count).AppendLine(" members")
                    .Append("Created by ");

                await AppendMember(ctx.Guild, group.CreatorId, sb).ConfigureAwait(false);
                sb.AppendLine();

                sb.Append("Expiring in ").AppendLine((group.CreatedAt.AddHours(MemberGroupDTO.MAX_LIFETIME_HOURS) - DateTimeOffset.UtcNow).ToString(@"hh\h\ mm\m"))
                    .AppendLine()
                    .AppendLine(Formatter.Bold("Members"));

                foreach (ulong userId in group.UserIds)
                {
                    await AppendMember(ctx.Guild, userId, sb).ConfigureAwait(false);
                    sb.Append(' ');
                }

                DiscordMessageBuilder builder = new();
                builder.WithContent(sb.ToString());
                builder.WithAllowedMentions(Mentions.None);

                await ctx.RespondAsync(builder).ConfigureAwait(false);
            }
            catch (Refit.ValidationApiException va) when (va.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await ctx.RespondWithErrorAsync("That group does not exist.").ConfigureAwait(false);
                return;
            }
        }

        private async Task AppendMember(DiscordGuild guild, ulong memberId, StringBuilder sb)
        {
            MemberReturnedInfo member = await guild.TryGetMemberAsync(memberId).ConfigureAwait(false);
            if (member.Status == MemberReturnedInfo.GetMemberStatus.Failure)
                sb.Append(Formatter.InlineCode("unknown"));
            else
                sb.Append(member.Member.Mention);
        }

        [Command("create")]
        [Description("Creates a new group from the given members")]
        public async Task CreateGroupCommand(
            CommandContext ctx,
            [Description("The unique name of the group")] string groupName,
            [Description("The members to include in the group")] params DiscordMember[] members)
        {
            if (string.IsNullOrEmpty(groupName) || groupName.Length < 3)
            {
                await ctx.RespondWithErrorAsync("The group name must be at least three characters in length.").ConfigureAwait(false);
                return;
            }

            if (members.Length > 50)
            {
                await ctx.RespondWithErrorAsync("A group cannot have more than 50 members.").ConfigureAwait(false);
                return;
            }

            members = members.Distinct().ToArray();

            if (members.Length < 2)
            {
                await ctx.RespondWithErrorAsync("A group cannot have less than two members, excluding duplicates.").ConfigureAwait(false);
                return;
            }

            MemberGroupDTO group = new(groupName, ctx.Guild.Id, ctx.User.Id, members.Select(m => m.Id).ToList());

            try
            {
                await DbApi.CreateMemberGroup(group).ConfigureAwait(false);
                await ctx.RespondWithSuccessAsync($"The group {Formatter.Bold(groupName)} has been created with {Formatter.Bold(members.Length.ToString())} members.").ConfigureAwait(false);
            }
            catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                await ctx.RespondWithErrorAsync("A group with this name already exists. Please try again with a different name.").ConfigureAwait(false);
            }
        }

        [Command("delete")]
        [Description("Deletes a group")]
        public async Task DeleteGroupCommand(
            CommandContext ctx,
            [Description("The unique name of the guild")] string groupName)
        {

        }
    }
}
