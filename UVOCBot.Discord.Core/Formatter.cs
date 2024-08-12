using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using UVOCBot.Discord.Core.Errors;

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
    public static string MaskedLink(string title, string url) => $"[{title}]({url})";
    public static string Underline(string content) => $"__{content}__";

    public static string Timestamp(long timestamp, TimestampStyle style)
        => $"<t:{ timestamp }:{ TimestampStyleToCode(style) }>";

    public static string Timestamp(DateTimeOffset dateTime, TimestampStyle style)
        => Timestamp(dateTime.ToUnixTimeSeconds(), style);

    public static string Timestamp(DateTime dateTime, TimestampStyle style)
        => Timestamp(new DateTimeOffset(dateTime).ToUnixTimeSeconds(), style);

    public static Result<IEmoji> EmojiFromString(string value)
    {
        // Sometimes Discord will escape emojis
        value = value.Trim('<', '>');
        if (value.Length < 3) // At least two colons and a name/id
            return new GenericCommandError("Emoji string was too short");

        bool isAnimated = value[0] is 'a';
        value = value.Trim('a');

        string[] parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 0)
            return new GenericCommandError("Emoji string format was invalid");

        Snowflake? id = null;
        if (parts.Length > 1)
        {
            if (!ulong.TryParse(parts[1], out ulong emoId))
                return new GenericCommandError("Emoji ID could not be parsed");
            id = DiscordSnowflake.New(emoId);
        }

        return new Emoji(id, parts[0], IsAnimated: isAnimated);
    }

    private static char TimestampStyleToCode(TimestampStyle style)
        => style switch
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
