using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace DSharpPlus.CommandsNext
{
    public static class CommandContextExtensions
    {
        public static async Task<DiscordMessage> RespondWithErrorAsync(this CommandContext ctx, string message)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = DiscordColor.Red,
                Description = message
            };
            return await ctx.RespondAsync(embed.Build()).ConfigureAwait(false);
        }

        public static async Task<DiscordMessage> RespondWithSuccessAsync(this CommandContext ctx, string message)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = DiscordColor.Green,
                Description = message
            };
            return await ctx.RespondAsync(embed.Build()).ConfigureAwait(false);
        }
    }
}
