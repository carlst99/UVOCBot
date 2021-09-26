using Remora.Discord.Core;
using System.Drawing;

namespace UVOCBot.Discord.Core
{
    public static class DiscordConstants
    {
        private static Snowflake _applicationId;
        private static Snowflake _userId;

        public static readonly Color DEFAULT_EMBED_COLOUR = Color.Purple;

        public static Snowflake ApplicationId
        {
            get => _applicationId == default
                ? throw new InvalidOperationException("This value has not yet been set")
                : _applicationId;
            set => _applicationId = value;
        }

        public static Snowflake UserId
        {
            get => _userId == default
                ? throw new InvalidOperationException("This value has not yet been set")
                : _userId;
            set => _userId = value;
        }
    }
}
