using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Group("twitter")]
    [Description("Commands pertinent to the twitter relay functionality")]
    public class TwitterModule : BaseCommandModule
    {
        [Command("add-user")]
        [Description("Adds a twitter user from whom tweets should be relayed")]
        public async Task AddUserCommand(CommandContext ctx, [Description("The person's twitter username, e.g. @Wrel")] string username)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);



            await ctx.RespondAsync($"{Program.NAME} is now relaying tweets from {username}!").ConfigureAwait(false);
        }
    }
}
