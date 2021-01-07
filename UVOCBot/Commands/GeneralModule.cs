﻿using DSharpPlus;
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
            await ctx.RespondAsync($"I'm version **{Assembly.GetEntryAssembly().GetName().Version}**!").ConfigureAwait(false);
        }

        [Command("bonk")]
        [Aliases("goToHornyJail")]
        [Description("Sends a voice member to horny jail")]
        [RequireGuild]
        [RequirePermissions(Permissions.MoveMembers)]
        public async Task BonkCommand(CommandContext ctx, DiscordMember memberToBonk)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            // Limit command abuse by making sure the user is actively in the voice channel
            if (ctx.Member.VoiceState.Channel is null)
            {
                await ctx.RespondAsync("You must be a voice channel to use this command").ConfigureAwait(false);
                return;
            }

            // Ensure that the target is part of the same voice channel
            if (!memberToBonk.VoiceState.Channel.Equals(ctx.Member.VoiceState.Channel))
            {
                await ctx.RespondAsync("You must be in the same voice channel as the member you are trying to bonk").ConfigureAwait(false);
                return;
            }

            // Check that UVOCBot can move members out of the current channel
            DiscordChannel currentAudioChannel = ctx.Member.VoiceState.Channel;

            if (!CheckMemberHasMovePermissionInChannel(currentAudioChannel, ctx.Guild.CurrentMember))
            {
                await ctx.RespondAsync($"{Program.NAME} does not have permissions to move members from your current channel").ConfigureAwait(false);
                return;
            }

            // Find or create the guild twitter settings record
            GuildSettingsDTO settings = await GetGuildSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);
            // Check that a bonk target has been set
            if (settings.BonkChannelId is null)
            {
                await ctx.RespondAsync($"You haven't yet setup a target voice channel for the bonk command. Please use {Program.PREFIX}bonk <channel>").ConfigureAwait(false);
                return;
            }

            // Check that the bonk target still exists
            DiscordChannel moveToChannel = ctx.Guild.GetChannel((ulong)settings.BonkChannelId);
            if (moveToChannel == default)
            {
                await ctx.RespondAsync($"The bonk voice chat no longer exists. Please reset it using {Program.PREFIX}bonk <channel>").ConfigureAwait(false);
                return;
            }

            if (!CheckMemberHasMovePermissionInChannel(moveToChannel, ctx.Guild.CurrentMember))
            {
                await ctx.RespondAsync($"{Program.NAME} does not have permissions to move members to the bonk channel").ConfigureAwait(false);
                return;
            }

            await moveToChannel.PlaceMemberAsync(memberToBonk).ConfigureAwait(false);
            await ctx.RespondAsync($"**{memberToBonk.DisplayName}** has been bonked :smirk::hammer:").ConfigureAwait(false);
        }

        [Command("bonk")]
        [RequireGuild]
        [RequirePermissions(Permissions.MoveMembers)]
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

            await ctx.RespondAsync($"The bonk channel has been successfully set to {bonkChannel.Mention}").ConfigureAwait(false);
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

        private bool CheckMemberHasMovePermissionInChannel(DiscordChannel channel, DiscordMember member)
        {
            Permissions permissions = channel.PermissionsFor(member);
            return (permissions & Permissions.MoveMembers) != 0;
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
