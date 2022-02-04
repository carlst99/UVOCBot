using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;

namespace UVOCBot.Discord.Core.Errors;

/// <summary>
/// Represents an error caused by missing permissions.
/// </summary>
/// <param name="Permission">The missing permission.</param>
/// <param name="UserID">The user that is missing the <paramref name="Permission"/>.</param>
/// <param name="ChannelID">The channel that the <paramref name="Permission"/> is required in.</param>
public record PermissionError
(
    DiscordPermission Permission,
    Snowflake UserID,
    Snowflake? ChannelID
) : ResultError(string.Empty)
{
    public override string ToString()
    {
        string userMention = Formatter.UserMention(UserID) + " doesn't";
        string permissionMention = Formatter.InlineQuote(Permission.ToString());

        string message = $"{ userMention } have the required { permissionMention } permission";

        if (ChannelID is not null)
        {
            string channelMention = Formatter.ChannelMention(ChannelID.Value);
            message += $" in {channelMention}";
        }

        return message + ".";
    }

    public string ContextualToString(ICommandContext context)
    {
        string userMention = UserID == context.User.ID ? "You don't" : Formatter.UserMention(UserID) + " doesn't";
        string permissionMention = Formatter.InlineQuote(Permission.ToString());

        string message = $"{ userMention } have the required { permissionMention } permission";

        if (ChannelID is not null)
        {
            string channelMention = ChannelID == context.ChannelID ? "this channel" : Formatter.ChannelMention(ChannelID.Value);
            message += $" in {channelMention}";
        }

        return message + ".";
    }
}
