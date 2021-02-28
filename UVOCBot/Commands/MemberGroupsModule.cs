using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Description("Commands that allow groups of members to be created")]
    [RequireGuild]
    [Group("group")]
    public class MemberGroupsModule : BaseCommandModule
    {
        [Command("create")]
        [Description("Creates a new group from the given members")]
        public async Task CreateGroupCommand(
            [Description("The unique name of the group")] string groupName,
            [Description("The members to include in the group")] params DiscordMember[] members)
        {

        }
    }
}
