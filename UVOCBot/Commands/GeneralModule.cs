using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    public class GeneralModule : BaseCommandModule
    {
        [Command("ping")]
        [Description("Pong! Tells you whether the bot is listening")]
        public async Task PingCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("pong!").ConfigureAwait(false);
        }

        [Command("coinflip")]
        [Description("Flips a coin")]
        public async Task CoinFlip(CommandContext ctx)
        {
            Random rnd = new Random();
            int result = rnd.Next(0, 2);
            if (result == 0)
                await ctx.RespondAsync("You flipped a **heads**!").ConfigureAwait(false);
            else
                await ctx.RespondAsync("You flipped a **tails**!").ConfigureAwait(false);
        }
    }
}
