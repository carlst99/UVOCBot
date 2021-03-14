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

        public static async Task RespondWithDMFailureMessage(this CommandContext ctx, string failureMessage = null)
        {
            if (string.IsNullOrEmpty(failureMessage))
            {
                await ctx.RespondAsync($"{ctx.Member.Mention} I need to send you a direct message, but you've either disabled them or blocked me." +
                " Please either, **unblock me** or adjust your *Privacy & Safety* settings to **Allow direct messages from server members**." +
                " You can do this for every server (general settings) or just this one (right-click on the icon -> Privacy Settings).").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync(failureMessage).ConfigureAwait(false);
            }
        }
    }
}
