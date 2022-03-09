using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;

namespace UVOCBot.Discord.Core.Errors;

/// <summary>
/// Represents an error caused by missing permissions.
/// </summary>
/// <param name="Permissions">The missing permissions.</param>
/// <param name="UserID">The user that is missing the <paramref name="Permissions"/>.</param>
/// <param name="ChannelID">The channel that the <paramref name="Permissions"/> is required in.</param>
public record PermissionError
(
    IReadOnlyList<DiscordPermission> Permissions,
    Snowflake UserID,
    Snowflake? ChannelID
) : ResultError(string.Empty)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionError"/> record.
    /// </summary>
    /// <param name="permission">The missing permission.</param>
    /// <param name="userID">The user that is missing the <paramref name="permission"/>.</param>
    /// <param name="channelID">The channel that the <paramref name="permission"/> is required in.</param>
    public PermissionError
    (
        DiscordPermission permission,
        Snowflake userID,
        Snowflake channelID
    ) : this
    (
        new[] {permission},
        userID,
        channelID
    )
    {
    }

    public override string ToString()
    {
        string userMention = Formatter.UserMention(UserID) + " doesn't";
        string permissionMention = Formatter.InlineQuote(string.Join(", ", Permissions));

        string message = $"{ userMention } have the required permission/s ({permissionMention})";

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
        string permissionMention = Formatter.InlineQuote(string.Join(", ", Permissions));

        string message = $"{ userMention } have the required permission/s ({permissionMention})";

        if (ChannelID is not null)
        {
            string channelMention = ChannelID == context.ChannelID ? "this channel" : Formatter.ChannelMention(ChannelID.Value);
            message += $" in {channelMention}";
        }

        return message + ".";
    }
}
