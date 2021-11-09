using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;

namespace UVOCBot.Discord.Core;

public static class Formatter
{
    public static string ChannelMention(ulong channelID) => $"<#{channelID}>";
    public static string ChannelMention(Snowflake channelID) => ChannelMention(channelID.Value);
    public static string ChannelMention(IChannel channel) => ChannelMention(channel.ID);

    public static string RoleMention(ulong roleID) => $"<@&{roleID}>";
    public static string RoleMention(Snowflake roleID) => RoleMention(roleID.Value);
    public static string RoleMention(IRole role) => RoleMention(role.ID);

    public static string UserMention(ulong userID) => $"<@{userID}>";
    public static string UserMention(Snowflake userID) => UserMention(userID.Value);
    public static string UserMention(IUser user) => UserMention(user.ID);

    public static string Bold(string content) => $"**{content}**";
    public static string CodeBlock(string content, string? language = null) => $"```{language}\n{content}\n```";
    public static string Emoji(string content) => $":{content}:";
    public static string InlineQuote(string content) => $"`{content}`";
    public static string Italic(string content) => $"*{content}*";
    public static string Quote(string content) => $">{content}\n";
    public static string Spoiler(string content) => $"||{content}||";
    public static string Strikethrough(string content) => $"~~{content}~~";
    public static string Timestamp(long timestamp, TimestampStyle style = TimestampStyle.ShortDate) => $"<t:{ timestamp }:{ TimestampStyleToCode(style) }>";
    public static string Underline(string content) => $"__{content}__";

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
