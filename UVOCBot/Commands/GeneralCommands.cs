using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;

namespace UVOCBot.Commands;

public class GeneralCommands : CommandGroup
{
    public const string RELEASE_NOTES =
        @"• Added the `online-friends` command
            Base capture notifications now show outfit members that were involved.";

    private readonly IDiscordRestUserAPI _userAPI;
    private readonly FeedbackService _feedbackService;
    private readonly Random _rndGen;

    public GeneralCommands
    (
        IDiscordRestUserAPI userAPI,
        FeedbackService feedbackService
    )
    {
        _userAPI = userAPI;
        _feedbackService = feedbackService;

        _rndGen = new Random();
    }

    [Command("coinflip")]
    [Description("Flips a coin")]
    public async Task<IResult> CoinFlipCommandAsync()
    {
        string description = _rndGen.Next(0, 2) == 0
            ? $"{ Formatter.Emoji("coin") } You flipped a { Formatter.Bold("heads") }! { Formatter.Emoji("coin") }"
            : $"{ Formatter.Emoji("coin") } You flipped a { Formatter.Bold("tails") }! { Formatter.Emoji("coin") }";

        Embed embed = new()
        {
            Colour = Color.Gold,
            Description = description
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("http-cat")]
    [Description("Posts a cat image that represents the given HTTP error code.")]
    [Deferred]
    public async Task<IResult> PostHttpCatCommandAsync([Description("The HTTP code.")][DiscordTypeHint(TypeHint.Integer)] int httpCode)
    {
        Embed embed = new()
        {
            Image = new EmbedImage($"https://http.cat/{httpCode}"),
            Footer = new EmbedFooter("Image from http.cat")
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("info")]
    [Description("Gets information about UVOCBot")]
    [Deferred]
    public async Task<IResult> InfoCommandAsync()
    {
        string? botAvatar = null;
        Optional<string> authorAvatar = new();

        Result<IUser> botUser = await _userAPI.GetCurrentUserAsync(CancellationToken).ConfigureAwait(false);
        if (botUser.IsSuccess)
        {
            Result<Uri> botAvatarURI = CDN.GetUserAvatarUrl(botUser.Entity, CDNImageFormat.PNG);
            if (botAvatarURI.IsSuccess)
                botAvatar = botAvatarURI.Entity.AbsoluteUri;
        }

        Result<IUser> authorUser = await _userAPI.GetUserAsync
        (
            DiscordSnowflake.New(165629177221873664),
            CancellationToken
        ).ConfigureAwait(false);

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
            Thumbnail = botAvatar is not null ? new EmbedThumbnail(botAvatar, Height: 96, Width: 96) : new Optional<IEmbedThumbnail>(),
            Footer = new EmbedFooter("Developed by FalconEye#1153", authorAvatar),
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Url = "https://github.com/carlst99/UVOCBot",
            Fields = new List<IEmbedField>
            {
                new EmbedField("Release Notes", RELEASE_NOTES)
            }
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("timestamp")]
    [Description("Generates a Discord timestamp.")]
    [Ephemeral]
    public async Task<IResult> TimestampCommand
    (
        [Description("The offset (in hours) from UTC that your given time is.")] double utcOffset,
        int? year = null, int? month = null, int? day = null, int? hour = null, int? minute = null,
        TimestampStyle style = TimestampStyle.ShortTime
    )
    {
        if (utcOffset is < -12 or > 14)
            return await _feedbackService.SendContextualErrorAsync("GMT offset must be between -12 and 14.", ct: CancellationToken).ConfigureAwait(false);

        DateTimeOffset time = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(utcOffset));

        year ??= time.Year;
        month ??= time.Month;
        day ??= time.Day;
        hour ??= time.Hour;
        minute ??= time.Minute;

        try
        {
            time = new DateTimeOffset((int)year, (int)month, (int)day, (int)hour, (int)minute, 0, TimeSpan.FromHours(utcOffset));
        }
        catch
        {
            return await _feedbackService.SendContextualErrorAsync("Invalid arguments!", ct: CancellationToken).ConfigureAwait(false);
        }

        string formattedTimestamp = Formatter.Timestamp(time.ToUnixTimeSeconds(), style);

        IResult sendResult = await _feedbackService.SendContextualNeutralAsync
        (
            $"{ formattedTimestamp }\n\n{ Formatter.InlineQuote(formattedTimestamp) }",
            ct: CancellationToken
        ).ConfigureAwait(false);

        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : sendResult;
    }
}
