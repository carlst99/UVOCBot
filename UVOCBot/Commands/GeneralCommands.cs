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
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    public class GeneralCommands : CommandGroup
    {
        public const string RELEASE_NOTES = "• **World `status` Command** - Check the territory control and status of a world and it's continents." +
            "\r\n• **`timestamp` command** - Get a snippet you can use to insert localised datetimes into messages." +
            "\r\n• Made the `population` command faster.";

        private readonly ICommandContext _context;
        private readonly ReplyService _responder;
        private readonly IDiscordRestUserAPI _userAPI;
        private readonly Random _rndGen;

        public GeneralCommands(ICommandContext context, ReplyService responder, IDiscordRestUserAPI userAPI)
        {
            _context = context;
            _responder = responder;
            _userAPI = userAPI;

            _rndGen = new Random();
        }

        [Command("timestamp")]
        [Description("Generates a Discord timestamp.")]
        public async Task<IResult> TimestampCommand(
            [Description("The offset (in hours) from GMT that your given timestamp is.")] double gmtOffset,
            int? year = null, int? month = null, int? day = null, int? hour = null, int? minute = null)
        {
            if (gmtOffset < -12 || gmtOffset > 14)
                return await _responder.RespondWithSuccessAsync(_context, "GMT offset must be between -12 and 14.", CancellationToken).ConfigureAwait(false);

            DateTimeOffset time = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(gmtOffset));

            year ??= time.Year;
            month ??= time.Month;
            day ??= time.Day;
            hour ??= time.Hour;
            minute ??= time.Minute;

            try
            {
                time = new((int)year, (int)month, (int)day, (int)hour, (int)minute, 0, TimeSpan.FromHours(gmtOffset));
            }
            catch
            {
                return await _responder.RespondWithUserErrorAsync(_context, "Invalid arguments!", CancellationToken).ConfigureAwait(false);
            }

            await _responder.RespondWithSuccessAsync(
                _context,
                $"{ Formatter.Timestamp(time.ToUnixTimeSeconds()) }\n\n{ Formatter.InlineQuote($"<t:{ time.ToUnixTimeSeconds() }>") }",
                CancellationToken).ConfigureAwait(false);

            return Result.FromSuccess();
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
                Colour = BotConstants.DEFAULT_EMBED_COLOUR,
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
