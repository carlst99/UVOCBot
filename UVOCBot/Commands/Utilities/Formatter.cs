using Remora.Discord.Core;
using UVOCBot.Model;

namespace UVOCBot.Commands
{
    public static class Formatter
    {
        public static string ChannelMention(ulong channelID) => $"<#{channelID}>";
        public static string ChannelMention(Snowflake channelID) => $"<#{channelID.Value}>";
        public static string RoleMention(ulong roleID) => $"<@&{roleID}>";
        public static string RoleMention(Snowflake roleID) => $"<@&{roleID.Value}>";
        public static string UserMention(ulong userID) => $"<@{userID}>";
        public static string UserMention(Snowflake userID) => $"<@{userID.Value}>";

        public static string Bold(string content) => $"**{content}**";
        public static string Underline(string content) => $"*{content}*";
        public static string Emoji(string content) => $":{content}:";
        public static string Spoiler(string content) => $"||{content}||";
        public static string InlineQuote(string content) => $"`{content}`";
        public static string Quote(string content) => $">{content}\n";
        public static string CodeBlock(string content, string? language = null) => $"```{language}{content}```";
        public static string Timestamp(long timestamp, TimestampStyle style = TimestampStyle.ShortDate) => $"<t:{ timestamp }:{ TimestampStyleToCode(style) }>";

        private static char TimestampStyleToCode(TimestampStyle style)
        {
            return style switch
            {
                TimestampStyle.ShortTime => 't',
                TimestampStyle.LongTime => 'T',
                TimestampStyle.ShortDate => 'd',
                TimestampStyle.LongDate => 'D',
                TimestampStyle.ShortDateTime => 'f',
                TimestampStyle.LongDateTime => 'F',
                TimestampStyle.RelativeTime => 'R',
                _ => TimestampStyleToCode(TimestampStyle.ShortDate)
            };
        }
    }
}
