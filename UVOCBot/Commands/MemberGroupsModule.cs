using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
        public async Task GetGroupsCommand(CommandContext ctx)
        {
            List<MemberGroupDTO> groups = await DbApi.GetAllGuildMemberGroups(ctx.Guild.Id).ConfigureAwait(false);

            StringBuilder sb = new();
            sb.Append("Showing ").Append(Formatter.InlineCode(groups.Count.ToString())).Append(" groups for ").AppendLine(Formatter.InlineCode(ctx.Guild.Name)).AppendLine();

            foreach (MemberGroupDTO g in groups)
            {
                sb.Append("• ").Append(Formatter.InlineCode(g.GroupName)).Append(" (").Append(g.UserIds.Count).Append(" members) - created by ");

                MemberReturnedInfo groupCreator = await ctx.Guild.TryGetMemberAsync(g.CreatorId).ConfigureAwait(false);
                if (groupCreator.Status == MemberReturnedInfo.GetMemberStatus.Failure)
                    sb.AppendLine("`unknown member`");
                else
                    sb.AppendLine(groupCreator.Member.Mention);
            }

            await ctx.RespondAsync(sb.ToString()).ConfigureAwait(false);
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

            if (members.Length < 2 || members.Length > 50)
            {
                await ctx.RespondWithErrorAsync("A group must contain between two and 50 members.").ConfigureAwait(false);
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
