using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace UVOCBot.Extensions
{
    public class CustomDateTimeOffsetConverter : IArgumentConverter<DateTimeOffset>
    {
        public string Format { get; }

        public CustomDateTimeOffsetConverter(string format)
        {
            Format = format;
        }

        public Task<Optional<DateTimeOffset>> ConvertAsync(string value, CommandContext ctx)
        {
            bool success = DateTimeOffset.TryParseExact(
                value,
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite,
                out DateTimeOffset result);

            if (success)
            {
                return Task.FromResult(Optional.FromValue(result));
            } else
            {
                return Task.FromResult(Optional.FromNoValue<DateTimeOffset>());
            }
        }
    }
}
