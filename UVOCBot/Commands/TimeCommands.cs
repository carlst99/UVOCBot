using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    public class TimeCommands : CommandGroup
    {
        private readonly IReplyService _replyService;

        public TimeCommands(IReplyService responder)
        {
            _replyService = responder;
        }

        [Command("timestamp")]
        [Description("Generates a Discord timestamp.")]
        [Ephemeral]
        public async Task<IResult> TimestampCommand
        (
            [Description("The offset (in hours) from UTC that your given time is.")] double utcOffset,
            int? year = null, int? month = null, int? day = null, int? hour = null, int? minute = null,
            TimestampStyle style = TimestampStyle.ShortDate
        )
        {
            if (utcOffset < -12 || utcOffset > 14)
                return await _replyService.RespondWithSuccessAsync("GMT offset must be between -12 and 14.", CancellationToken).ConfigureAwait(false);

            DateTimeOffset time = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(utcOffset));

            year ??= time.Year;
            month ??= time.Month;
            day ??= time.Day;
            hour ??= time.Hour;
            minute ??= time.Minute;

            try
            {
                time = new((int)year, (int)month, (int)day, (int)hour, (int)minute, 0, TimeSpan.FromHours(utcOffset));
            }
            catch
            {
                return await _replyService.RespondWithUserErrorAsync("Invalid arguments!", CancellationToken).ConfigureAwait(false);
            }

            return await SendTimestampEmbed(time, style).ConfigureAwait(false);
        }

        // [Command("timestamp-format")]
        [Description("Replaces keys in a message with the given timestamps. Run without arguments to get more information.")]
        public async Task<IResult> TimestampFormatCommand
        (
            [Description("The message to format timestamps into.")] Snowflake? messageId = null,
            [Description("The timestamps to substitute in, concatenated with a pipe character ( | ) if multiple are required.")] string? timestamps = null
        )
        {
            if (messageId is null || timestamps is null)
            {
                return await _replyService.RespondWithSuccessAsync
                (
                    $"Pre-generate any timestamp tokens you need with the { Formatter.InlineQuote("timestamp") } command." +
                    "\r\nPost the message you'd like to format anywhere you'd like." +
                    $"\r\n\r\nTo indicate where you'd like a timestamp to be inserted, use curly brackets and the number of the timestamp you'd like to insert, i.e. { Formatter.InlineQuote("{1}") } This number comes from the index of the timestamps you submit in the respective argument." +
                    $"\r\n\r\nYou can offset the timestamp by using { Formatter.InlineQuote("+/-") }, a number value, and a code: y (year), M (month), d (day), h (hour), m (minute), s (second) i.e. { Formatter.InlineQuote("{1+1d-1h}") }" +
                    $"\r\n\r\nFurthermore, you can append a style as found here -> https://discord.com/developers/docs/reference#message-formatting-timestamp-styles, i.e. { Formatter.InlineQuote("{1+1d-1h:t}") }. If you don't append a style, the existing style on the submitted timestamps will be used." +
                    "\r\n\r\nFinally, copy the ID of the message you wish to be formatted by right-clicking it, and re-run this command while supplying the ID. Make sure you do this in the channel that you posted the message to.",
                    CancellationToken
                ).ConfigureAwait(false);
            }

            // TODO: Strip <> from submitted timestamps
            // TODO: Write parser
            // TODO: Write method to replace style if required
            // TODO: Natural parsing with DateTimeOffset.Parse?

            return Result.FromSuccess();
        }

        private async Task<Result<IMessage>> SendTimestampEmbed(DateTimeOffset timestamp, TimestampStyle style)
        {
            string formattedTimestamp = Formatter.Timestamp(timestamp.ToUnixTimeSeconds(), style);

            return await _replyService.RespondWithSuccessAsync
            (
                $"{ formattedTimestamp }\n\n{ Formatter.InlineQuote(formattedTimestamp) }",
                CancellationToken
            ).ConfigureAwait(false);
        }
    }
}
