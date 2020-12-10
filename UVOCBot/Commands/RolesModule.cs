using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Group("roles")]
    [Aliases("role", "r")]
    [Description("Commands pertinent to role management")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public class RolesModule : BaseCommandModule
    {
        [Command("add-by-reaction")]
        [Aliases("abr", "by-reaction")]
        [Description("Gives the specified emoji to all users who have reacted to a message with a particular emoji")]
        public async Task AddByReactionCommand(
            CommandContext ctx,
            [Description("The channel that the message was sent in")] DiscordChannel channel,
            [Description("The ID of the message")] ulong messageId,
            [Description("The role that should be assigned to each user")] DiscordRole role,
            [Description("The reaction emoji")] DiscordEmoji emoji)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            DiscordMessage message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (message is null)
            {
                await ctx.RespondAsync("Could not get the provided message. Please ensure you copied the right ID, and that I have permissions to view the channel that the message was posted in").ConfigureAwait(false);
                return;
            }
        }
    }
}
