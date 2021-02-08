using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    public class GeneralModule : BaseCommandModule
    {
        public const string RELEASE_NOTES = "- Added a Planetside 2 server status/population command: **ub!help planetside server**";

        public IApiService DbApi { get; set; }

        [Command("ping")]
        [Description("Pong! Tells you whether the bot is listening")]
        public async Task PingCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("pong!").ConfigureAwait(false);
        }

        [Command("coinflip")]
        [Description("Flips a coin")]
        public async Task CoinFlipCommand(CommandContext ctx)
        {
            Random rnd = new Random();
            int result = rnd.Next(0, 2);
            if (result == 0)
                await ctx.RespondAsync("You flipped a **heads**!").ConfigureAwait(false);
            else
                await ctx.RespondAsync("You flipped a **tails**!").ConfigureAwait(false);
        }

        [Command("version")]
        [Description("Gets the current version of this instance of UVOCBot")]
        public async Task VersionCommand(CommandContext ctx)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder
            {
                Title = $"Version {Assembly.GetEntryAssembly().GetName().Version}",
                Color = DiscordColor.Purple,
                Timestamp = DateTimeOffset.Now
            };
            builder.AddField("Release Notes", RELEASE_NOTES);

            await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("bonk")]
        [Aliases("goToHornyJail")]
        [Description("Sends a voice member to horny jail")]
        [RequireGuild]
        [RequirePermissions(Permissions.MoveMembers | Permissions.SendMessages)]
        public async Task BonkCommand(CommandContext ctx, [Description("The member to bonk")] DiscordMember memberToBonk)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Limit command abuse by making sure the user is actively in the voice channel
            if (ctx.Member.VoiceState.Channel is null)
            {
                await ctx.RespondAsync("You must be in a voice channel to use this command").ConfigureAwait(false);
                return;
            }

            // Ensure that the target is part of the same voice channel as the sender
            if (!memberToBonk.VoiceState.Channel.Equals(ctx.Member.VoiceState.Channel))
            {
                await ctx.RespondAsync("Bonking can only be used on members in the same voice channel").ConfigureAwait(false);
                return;
            }

            // Check that UVOCBot can move members out of the current channel
            if (!CheckPermission(ctx.Member.VoiceState.Channel, ctx.Guild.CurrentMember, Permissions.MoveMembers))
            {
                await ctx.RespondAsync($"{Program.NAME} does not have permissions to move members from your current channel").ConfigureAwait(false);
                return;
            }

            // Find or create the guild settings record
            GuildSettingsDTO settings = await GetGuildSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);

            // Check that a bonk channel has been set
            if (settings.BonkChannelId is null)
            {
                await ctx.RespondAsync($"You haven't yet setup a target voice channel for the bonk command. Please use {Program.PREFIX}bonk <channel>").ConfigureAwait(false);
                return;
            }

            // Check that the bonk channel still exists
            DiscordChannel bonkChannel = ctx.Guild.GetChannel((ulong)settings.BonkChannelId);
            if (bonkChannel == default)
            {
                await ctx.RespondAsync($"The bonk voice chat no longer exists. Please reset it using {Program.PREFIX}bonk <channel>").ConfigureAwait(false);
                return;
            }

            // Check that UVOCBot can move members into the bonk channel
            if (!CheckPermission(bonkChannel, ctx.Guild.CurrentMember, Permissions.MoveMembers))
            {
                await ctx.RespondAsync($"{Program.NAME} does not have permissions to move members to the bonk channel").ConfigureAwait(false);
                return;
            }

            await bonkChannel.PlaceMemberAsync(memberToBonk).ConfigureAwait(false);
            await ctx.RespondAsync($"**{memberToBonk.DisplayName}** has been bonked :smirk::hammer:").ConfigureAwait(false);
        }

        [Command("bonk-channel")]
        [Description("Sets the voice channel for the bonk command")]
        [RequireGuild]
        [RequirePermissions(Permissions.MoveMembers | Permissions.SendMessages)]
        public async Task BonkCommand(CommandContext ctx, [Description("The voice channel to send members to when they are bonked")] DiscordChannel bonkChannel)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            if (bonkChannel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("The bonk channel must be a voice channel!").ConfigureAwait(false);
                return;
            }

            GuildSettingsDTO settings = await GetGuildSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);
            settings.BonkChannelId = bonkChannel.Id;
            await DbApi.UpdateGuildSettings(settings.GuildId, settings).ConfigureAwait(false);

            await ctx.RespondAsync($"The bonk channel has been successfully set to **{bonkChannel.Name}**").ConfigureAwait(false);
        }

#if DEBUG
        [Command("test-embed")]
        [RequireOwner]
        public async Task TestEmbedCommand(CommandContext ctx)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Aquamarine,
                Description = "Test Description",
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Test Footer" },
                Author = new DiscordEmbedBuilder.EmbedAuthor { Name = "Test Author" },
                Timestamp = DateTimeOffset.UtcNow,
                Title = "Test Title"
            };
            builder.AddField("TestFieldName", "TestFieldValue");
            builder.AddField("TestFieldName2", "TestFieldValue2");
            builder.AddField("TestInlineFieldName", "TestInlineFieldValue", true);
            builder.AddField("TestInlineFieldName2", "TestInlineFieldValue2", true);
            await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Checks that a member has been granted a certain permission in a channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="member"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        private bool CheckPermission(DiscordChannel channel, DiscordMember member, Permissions permission)
        {
            Permissions permissions = channel.PermissionsFor(member);
            return (permissions & permission) != 0;
        }

        private async Task<GuildSettingsDTO> GetGuildSettingsAsync(ulong id)
        {
            GuildSettingsDTO settings;
            try
            {
                settings = await DbApi.GetGuildSetting(id).ConfigureAwait(false);
            }
            catch
            {
                settings = new GuildSettingsDTO(id);
                await DbApi.CreateGuildSettings(settings).ConfigureAwait(false);
            }

            return settings;
        }
    }
}
