using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    public class GeneralCommands : CommandGroup
    {
        public const string RELEASE_NOTES = "• **Slash Commands :tada:** - Everyone hates having to use `help` every five seconds to remember how to use each command. So I removed it, then set it on :fire: for good measure. Now, you can use Discord's new slash commands with UVOCBot! Rejoice!" +
            "\r\n• **Bug Fixes:tm:** - *Cough* (we'll see)";

        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly Random _rndGen;

        public GeneralCommands(ICommandContext context, MessageResponseHelpers responder, IDiscordRestUserAPI userAPI)
        {
            _context = context;
            _responder = responder;
            _userAPI = userAPI;

            _rndGen = new Random();
        }

        [Command("coinflip")]
        [Description("Flips a coin")]
        public async Task<IResult> CoinFlipCommandAsync()
        {
            string description;
            if (_rndGen.Next(0, 2) == 0)
                description = $"{Formatter.Emoji("coin")} You flipped a {Formatter.Bold("heads")}! {Formatter.Emoji("coin")}";
            else
                description = $"{Formatter.Emoji("coin")} You flipped a {Formatter.Bold("tails")}! {Formatter.Emoji("coin")}";

            Embed embed = new()
            {
                Colour = Color.Gold,
                Description = description
            };

            return await _responder.RespondWithEmbedAsync(_context, embed, CancellationToken).ConfigureAwait(false);
        }

        [Command("http-cat")]
        [Description("Posts a cat image that represents the given HTTP error code.")]
        public async Task<IResult> PostHttpCatCommandAsync([Description("The HTTP code.")] [DiscordTypeHint(TypeHint.Integer)] int httpCode)
        {
            var embed = new Embed
            {
                Image = new EmbedImage($"https://http.cat/{httpCode}"),
                Footer = new EmbedFooter("Image from http.cat")
            };

            return await _responder.RespondWithEmbedAsync(_context, embed, CancellationToken).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Gets information about UVOCBot")]
        public async Task<IResult> InfoCommandAsync()
        {
            Optional<string> botAvatar = new();
            Optional<string> authorAvatar = new();

            Result<IUser> botUser = await _userAPI.GetCurrentUserAsync(CancellationToken).ConfigureAwait(false);
            if (botUser.IsSuccess)
            {
                Result<Uri> botAvatarURI = CDN.GetUserAvatarUrl(botUser.Entity, CDNImageFormat.PNG);
                if (botAvatarURI.IsSuccess)
                    botAvatar = botAvatarURI.Entity.AbsoluteUri;
            }

            Result<IUser> authorUser = await _userAPI.GetUserAsync(new Snowflake(165629177221873664), CancellationToken).ConfigureAwait(false);
            if (authorUser.IsSuccess)
            {
                Result<Uri> authorAvatarURI = CDN.GetUserAvatarUrl(authorUser.Entity, CDNImageFormat.PNG);
                if (authorAvatarURI.IsSuccess)
                    authorAvatar = authorAvatarURI.Entity.AbsoluteUri;
            }

            Embed embed = new()
            {
                Title = "UVOCBot",
                Description = "A general-purpose bot built to assist the UVOC Discord server",
                Thumbnail = new EmbedThumbnail(botAvatar, Height: 96, Width: 96),
                Author = new EmbedAuthor("Written by FalconEye#1153", IconUrl: authorAvatar),
                Footer = new EmbedFooter($"Version {Assembly.GetEntryAssembly()?.GetName().Version}"),
                Colour = Program.DEFAULT_EMBED_COLOUR,
                Url = "https://github.com/carlst99/UVOCBot",
                Fields = new List<IEmbedField>
                {
                    new EmbedField("Release Notes", RELEASE_NOTES)
                }
            };

            return await _responder.RespondWithEmbedAsync(_context, embed, CancellationToken).ConfigureAwait(false);
        }
    }
}
