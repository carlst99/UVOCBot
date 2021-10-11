using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace UVOCBot.Discord.Core.Errors
{
    public record PermissionError : ResultError
    {
        /// <summary>
        /// The required permission.
        /// </summary>
        public DiscordPermission Permission { get; init; }

        /// <summary>
        /// The user that doesn't have the required permission.
        /// </summary>
        public Snowflake UserID { get; init; }

        /// <summary>
        /// The channel in which the permission is required.
        /// </summary>
        public Snowflake ChannelID { get; init; } // TODO: Nullify

        public PermissionError(DiscordPermission permission, Snowflake userID, Snowflake channelID, string? Message = null)
            : base(Message ?? string.Empty)
        {
            Permission = permission;
            UserID = userID;
            ChannelID = channelID;
        }

        public PermissionError(DiscordPermission permission, Snowflake userID, Snowflake channelID, ResultError original)
            : base(original)
        {
            Permission = permission;
            UserID = userID;
            ChannelID = channelID;
        }
    }
}
