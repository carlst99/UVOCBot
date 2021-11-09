using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands;

public class GeneralCommands : CommandGroup
{
    public const string RELEASE_NOTES = "• The `status` command is now much faster, and shows active alerts. Reliability is yet to be determined :stuck_out_tongue:.\n• Made a general sweep to improve stability and error feedback.";

    private readonly IReplyService _replyService;
    private readonly IDiscordRestUserAPI _userAPI;
    private readonly Random _rndGen;

    public GeneralCommands(IReplyService responder, IDiscordRestUserAPI userAPI)
    {
        _replyService = responder;
        _userAPI = userAPI;

        _rndGen = new Random();
    }

    [Command("coinflip")]
    [Description("Flips a coin")]
    public async Task<IResult> CoinFlipCommandAsync()
    {
        string description;
        if (_rndGen.Next(0, 2) == 0)
            description = $"{ Formatter.Emoji("coin") } You flipped a { Formatter.Bold("heads") }! { Formatter.Emoji("coin") }";
        else
            description = $"{ Formatter.Emoji("coin") } You flipped a { Formatter.Bold("tails") }! { Formatter.Emoji("coin") }";

        Embed embed = new()
        {
            Colour = Color.Gold,
            Description = description
        };

        return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
    }

    [Command("http-cat")]
    [Description("Posts a cat image that represents the given HTTP error code.")]
    public async Task<IResult> PostHttpCatCommandAsync([Description("The HTTP code.")][DiscordTypeHint(TypeHint.Integer)] int httpCode)
    {
        var embed = new Embed
        {
            Image = new EmbedImage($"https://http.cat/{httpCode}"),
            Footer = new EmbedFooter("Image from http.cat")
        };

        return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
    }

    [Command("info")]
    [Description("Gets information about UVOCBot")]
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
            Thumbnail = botAvatar is not null ? new EmbedThumbnail(botAvatar, Height: 96, Width: 96) : new Optional<IEmbedThumbnail>(),
            Author = new EmbedAuthor("Written by FalconEye#1153", IconUrl: authorAvatar),
            Footer = new EmbedFooter($"Version {Assembly.GetEntryAssembly()?.GetName().Version}"),
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Url = "https://github.com/carlst99/UVOCBot",
            Fields = new List<IEmbedField>
                {
                    new EmbedField("Release Notes", RELEASE_NOTES)
                }
        };

        return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
    }
}
