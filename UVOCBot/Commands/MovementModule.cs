using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [RequireGuild]
    [RequirePermissions(Permissions.MoveMembers)]
    public class MovementModule : BaseCommandModule
    {
        private static readonly Dictionary<int, string> INT_TO_EMOJI_STRING_TABLE = new Dictionary<int, string>
        {
            { 1, ":one:" },
            { 2, ":two:" },
            { 3, ":three:" },
            { 4, ":four:" },
            { 5, ":five:" }
        };

        private static readonly Dictionary<string, int> EMOJI_STRING_TO_INT_TABLE = new Dictionary<string, int>
        {
            { ":one:", 1 },
            { ":two:", 2 },
            { ":three:", 3 },
            { ":four:", 4 },
            { ":five:", 5 }
        };

        [Command("move")]
        [Description("Moves everyone from the voice channel the command caller is currently connected to to another")]
        public async Task MoveCommand(
            CommandContext ctx,
            [Description("The voice channel name/ID to move people to. Including a partial channel name will search for the most relevant channel")] string moveTo)
        {
            if (ctx.Member.VoiceState is null || ctx.Member.VoiceState.Channel is null)
            {
                await ctx.RespondWithErrorAsync("You are not connected to a voice channel. Please also specifiy a channel to move people out of, i.e. " + Formatter.InlineCode("move <moveFrom> <moveTo>")).ConfigureAwait(false);
                return;
            }

            DiscordChannel moveToChannel = await ResolveChannel(ctx, moveTo).ConfigureAwait(false);
            if (moveToChannel is null)
                return;

            foreach (DiscordMember user in ctx.Member.VoiceState.Channel.Users)
                await moveToChannel.PlaceMemberAsync(user).ConfigureAwait(false);

            await ctx.RespondAsync(":white_check_mark:").ConfigureAwait(false);
        }

        [Command("move")]
        [Description("Moves everyone from one voice channel to another")]
        public async Task MoveCommand(
            CommandContext ctx,
            string moveFrom,
            string moveTo)
        {
            DiscordChannel moveFromChannel = await ResolveChannel(ctx, moveFrom).ConfigureAwait(false);
            if (moveFromChannel is null)
                return;

            DiscordChannel moveToChannel = await ResolveChannel(ctx, moveTo).ConfigureAwait(false);
            if (moveToChannel is null)
                return;

            foreach (DiscordMember user in moveFromChannel.Users)
                await moveToChannel.PlaceMemberAsync(user).ConfigureAwait(false);

            await ctx.RespondAsync(":white_check_mark:").ConfigureAwait(false);
        }

        private async Task<DiscordChannel> ResolveChannel(CommandContext ctx, string channelName)
        {
            if (ulong.TryParse(channelName, out ulong channelId) && ctx.Guild.Channels.ContainsKey(channelId) && ctx.Guild.Channels[channelId].Type == ChannelType.Voice)
                return ctx.Guild.Channels[channelId];

            IEnumerable<DiscordChannel> validChannels = ctx.Guild.Channels.Values.Where(c => c.Name.StartsWith(channelName) && c.Type == ChannelType.Voice);

            int channelCount = validChannels.Count();
            if (channelCount == 0)
            {
                await ctx.RespondWithErrorAsync("Could not find a relevant voice channel.").ConfigureAwait(false);
                return null;
            }
            else if (channelCount == 1)
            {
                return validChannels.Single();
            }
            else if (channelCount > 5)
            {
                await ctx.RespondWithErrorAsync("More than five relevant voice channels found. Please provide a more specific channel name (or ID).").ConfigureAwait(false);
                return null;
            }
            else
            {
                StringBuilder sb = new();
                int i = 1;
                foreach (DiscordChannel c in validChannels)
                {
                    sb.Append(i).Append(". ").AppendLine(c.Name);
                    i++;
                }

                DiscordEmbedBuilder builder = new()
                {
                    Title = "Select Channel",
                    Description = sb.ToString()
                };
                DiscordMessage message = await ctx.RespondAsync(builder.Build()).ConfigureAwait(false);

                for (i = 0; i < channelCount; i++)
                    await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, INT_TO_EMOJI_STRING_TABLE[i + 1], false)).ConfigureAwait(false);

                InteractivityResult<MessageReactionAddEventArgs> reaction = await message.WaitForReactionAsync(ctx.User).ConfigureAwait(false);
                if (reaction.TimedOut)
                    return null;

                string name = reaction.Result.Emoji.GetDiscordName();
                int position = EMOJI_STRING_TO_INT_TABLE[name];
                return validChannels.ElementAt(position - 1);
            }
        }
    }
}
