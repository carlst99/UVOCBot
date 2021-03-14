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
using UVOCBot.Core.Model;
using UVOCBot.Extensions;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    [RequireGuild]
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

        public IApiService DbApi { get; set; }

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

            DiscordChannel moveToChannel = await TryResolveChannelAsync(ctx, moveTo).ConfigureAwait(false);
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
            [Description("The voice channel name/ID to move people from. Including a partial channel name will search for the most relevant channel")] string moveFrom,
            [Description("The voice channel name/ID to move people to. Including a partial channel name will search for the most relevant channel")] string moveTo)
        {
            DiscordChannel moveFromChannel = await TryResolveChannelAsync(ctx, moveFrom).ConfigureAwait(false);
            if (moveFromChannel is null)
                return;

            DiscordChannel moveToChannel = await TryResolveChannelAsync(ctx, moveTo).ConfigureAwait(false);
            if (moveToChannel is null)
                return;

            foreach (DiscordMember user in moveFromChannel.Users)
                await moveToChannel.PlaceMemberAsync(user).ConfigureAwait(false);

            await ctx.RespondAsync(":white_check_mark:").ConfigureAwait(false);
        }

        [Command("move-group")]
        [Description("Moves a group from one voice channel to another")]
        public async Task MoveGroupCommand(
            CommandContext ctx,
            [Description("The member group to move. See the group commands for more info")] string groupName,
            [Description("The voice channel name/ID to move people to. Including a partial channel name will search for the most relevant channel")] string moveTo)
        {
            if (ctx.Member.VoiceState is null || ctx.Member.VoiceState.Channel is null)
            {
                await ctx.RespondWithErrorAsync("You are not connected to a voice channel. Please also specifiy a channel to move people out of, i.e. " + Formatter.InlineCode("move <moveFrom> <moveTo>")).ConfigureAwait(false);
                return;
            }

            DiscordChannel moveToChannel = await TryResolveChannelAsync(ctx, moveTo).ConfigureAwait(false);
            if (moveToChannel is null)
                return;

            List<DiscordMember> groupMembers = await TryGetGroupMembersAsync(ctx, groupName).ConfigureAwait(false);
            foreach (DiscordMember member in groupMembers)
            {
                if (ctx.Member.VoiceState.Channel.Users.Contains(member))
                    await member.PlaceInAsync(moveToChannel).ConfigureAwait(false);
            }

            await ctx.RespondAsync(":white_check_mark:").ConfigureAwait(false);
        }

        [Command("move-group")]
        [Description("Moves a group from one voice channel to another")]
        public async Task MoveGroupCommand(
            CommandContext ctx,
            [Description("The member group to move. See the group commands for more info")] string groupName,
            [Description("The voice channel name/ID to move people from. Including a partial channel name will search for the most relevant channel")] string moveFrom,
            [Description("The voice channel name/ID to move people to. Including a partial channel name will search for the most relevant channel")] string moveTo)
        {
            DiscordChannel moveFromChannel = await TryResolveChannelAsync(ctx, moveFrom).ConfigureAwait(false);
            if (moveFromChannel is null)
                return;

            DiscordChannel moveToChannel = await TryResolveChannelAsync(ctx, moveTo).ConfigureAwait(false);
            if (moveToChannel is null)
                return;

            List<DiscordMember> groupMembers = await TryGetGroupMembersAsync(ctx, groupName).ConfigureAwait(false);
            foreach (DiscordMember member in groupMembers)
            {
                if (moveFromChannel.Users.Contains(member))
                    await member.PlaceInAsync(moveToChannel).ConfigureAwait(false);
            }

            await ctx.RespondAsync(":white_check_mark:").ConfigureAwait(false);
        }

        private async Task<DiscordChannel> TryResolveChannelAsync(CommandContext ctx, string channelName)
        {
            if (ulong.TryParse(channelName, out ulong channelId) && ctx.Guild.Channels.ContainsKey(channelId))
            {
                DiscordChannel channel = ctx.Guild.Channels[channelId];
                if (channel.Type == ChannelType.Voice && channel.MemberHasPermissions(Permissions.MoveMembers, ctx.Member, ctx.Guild.CurrentMember))
                    return channel;
            }

            IEnumerable<DiscordChannel> validChannels = ctx.Guild.Channels.Values.Where(c => c.Name.StartsWith(channelName)
                && c.Type == ChannelType.Voice
                && c.MemberHasPermissions(Permissions.MoveMembers, ctx.Member, ctx.Guild.CurrentMember));

            int channelCount = validChannels.Count();
            if (channelCount == 0)
            {
                await ctx.RespondWithErrorAsync("Could not find a relevant voice channel (excluding those for which you or I don't have permissions to move members).").ConfigureAwait(false);
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

        private async Task<List<DiscordMember>> TryGetGroupMembersAsync(CommandContext ctx, string groupName)
        {
            try
            {
                List<DiscordMember> groupMembers = new();

                MemberGroupDTO group = await DbApi.GetMemberGroup(ctx.Guild.Id, groupName).ConfigureAwait(false);

                foreach (ulong userId in group.UserIds)
                {
                    MemberReturnedInfo mri = await ctx.Guild.TryGetMemberAsync(userId).ConfigureAwait(false);
                    if (mri.Status == MemberReturnedInfo.GetMemberStatus.Success)
                        groupMembers.Add(mri.Member);
                }

                return groupMembers;
            }
            catch (Refit.ValidationApiException vaex) when (vaex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await ctx.RespondWithErrorAsync("That group doesn't exist.").ConfigureAwait(false);
                return null;
            }
        }
    }
}
