using Remora.Discord.Core;

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
    }
}
