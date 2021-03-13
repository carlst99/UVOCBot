﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UVOCBot.Config;
using UVOCBot.Core.Model;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    public class GeneralModule : BaseCommandModule
    {
        public const string RELEASE_NOTES = "- **Custom prefixes!** Set a prefix to trigger commands using `ub!prefix`" +
            "\r\n- **Purple embeds** - Standardised the embed colour to purple" +
            "\r\n- **Default PlanetSide server** - PlanetSide commands that require a server can now do away with this argument. Use `ub!default-server` to set your default";

        public IApiService DbApi { get; set; }
        public IPrefixService PrefixService { get; set; }

        public IOptions<GeneralOptions> GOptions { get; set; }
        public GeneralOptions GeneralOptions => GOptions.Value;

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

        [Command("about")]
        [Aliases("info", "version")]
        [Description("Gets information about UVOCBot")]
        public async Task VersionCommand(CommandContext ctx)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder
            {
                Title = "UVOCBot",
                Description = "A general-purpose bot providing functions to assist with the gaming (in particular, PlanetSide 2) experience of a Discord server",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = "https://cdn.discordapp.com/app-icons/747683069737041970/91cb442e9f1811fabfa7611e4b564acd.png",
                    Height = 96,
                    Width = 96
                },
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = "Written by Carl, A.K.A FalconEye",
                    Url = "https://github.com/carlst99",
                    IconUrl = "https://cdn.discordapp.com/avatars/165629177221873664/c1bb057dd76dfec6ed8c2de62dc1c185.png"
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"Version {Assembly.GetEntryAssembly().GetName().Version}"
                },
                Color = Program.DEFAULT_EMBED_COLOUR,
                Timestamp = DateTimeOffset.Now,
                Url = "https://github.com/carlst99/UVOCBot"
            };

            builder.AddField("Prefix", Formatter.InlineCode(PrefixService.GetPrefix(ctx.Guild.Id)));
            builder.AddField("Release Notes", RELEASE_NOTES);

            await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }
        [Command("prefix")]
        [Description("Removes your custom prefix")]
        [RequireGuild]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task PrefixCommand(CommandContext ctx)
        {
            await PrefixService.RemovePrefixAsync(ctx.Guild.Id).ConfigureAwait(false);
            await ctx.RespondAsync($"Your prefix has been unset. You can trigger commands with `{GeneralOptions.CommandPrefix}`").ConfigureAwait(false);
        }

        [Command("prefix")]
        [Description("Lets you set a custom prefix with which to trigger commands")]
        [RequireGuild]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task PrefixCommand(CommandContext ctx, string prefix)
        {
            await PrefixService.UpdatePrefixAsync(ctx.Guild.Id, prefix).ConfigureAwait(false);
            await ctx.RespondAsync($"You can now trigger commands with the prefix `{prefix}`").ConfigureAwait(false);
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
                await ctx.RespondAsync($"{ctx.Guild.CurrentMember.DisplayName} does not have permissions to move members from your current channel").ConfigureAwait(false);
                return;
            }

            // Find or create the guild settings record
            GuildSettingsDTO settings = await DbApi.GetGuildSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);

            // Check that a bonk channel has been set
            if (settings.BonkChannelId is null)
            {
                await ctx.RespondAsync($"You haven't yet setup a target voice channel for the bonk command. Please use {GeneralOptions.CommandPrefix}bonk <channel>").ConfigureAwait(false);
                return;
            }

            // Check that the bonk channel still exists
            DiscordChannel bonkChannel = ctx.Guild.GetChannel((ulong)settings.BonkChannelId);
            if (bonkChannel == default)
            {
                await ctx.RespondAsync($"The bonk voice chat no longer exists. Please reset it using {GeneralOptions.CommandPrefix}bonk <channel>").ConfigureAwait(false);
                return;
            }

            // Check that UVOCBot can move members into the bonk channel
            if (!CheckPermission(bonkChannel, ctx.Guild.CurrentMember, Permissions.MoveMembers))
            {
                await ctx.RespondAsync($"{ctx.Guild.CurrentMember.DisplayName} does not have permissions to move members to the bonk channel").ConfigureAwait(false);
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

            GuildSettingsDTO settings = await DbApi.GetGuildSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);
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
                Color = Program.DEFAULT_EMBED_COLOUR,
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

        [Command("throw-exception")]
        [RequireOwner]
        public Task ThrowExceptionCommand(CommandContext ctx)
        {
            throw new Exception();
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
    }
}
