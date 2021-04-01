using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;

namespace UVOCBotRemora.Commands
{
    public class GeneralCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly CommandContextReponses _responder;
        private readonly Random _rndGen;

        public GeneralCommands(ICommandContext context, CommandContextReponses responder)
        {
            _context = context;
            _responder = responder;

            _rndGen = new Random();
        }

        [Command("coinflip")]
        [Description("Flips a coin")]
        public async Task<IResult> CoinFlipCommandAsync()
        {
            int result = _rndGen.Next(0, 2);
            Embed embed;

            if (result == 0)
            {
                embed = new Embed
                {
                    Colour = Color.Gold,
                    Description = $"{Formatter.Emoji("coin")} You flipped a {Formatter.Bold("heads")}! {Formatter.Emoji("coin")}"
                };
            }
            else
            {
                embed = new Embed
                {
                    Colour = Color.Gold,
                    Description = $"{Formatter.Emoji("coin")} You flipped a {Formatter.Bold("tails")}! {Formatter.Emoji("coin")}"
                };
            }

            Result<IMessage> reply = await _responder.RespondAsync(_context, embed: embed, ct: CancellationToken).ConfigureAwait(false);

            return !reply.IsSuccess
                    ? Result.FromError(reply)
                    : Result.FromSuccess();
        }

        [Command("http-cat")]
        [Description("Posts a cat image that represents the given HTTP error code.")]
        public async Task<IResult> PostHttpCatAsync([Description("The HTTP code.")] [DiscordTypeHint(TypeHint.Integer)] int httpCode)
        {
            var embedImage = new EmbedImage($"https://http.cat/{httpCode}");
            var embedFooter = new EmbedFooter("Image from http.cat");

            var embed = new Embed
            {
                Image = embedImage,
                Footer = embedFooter
            };

            Result<IMessage> reply = await _responder.RespondAsync(_context, embed: embed, ct: CancellationToken).ConfigureAwait(false);

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }
    }
}
