using DbgCensus.EventStream.Commands;
using Remora.Discord.Core;
using System;
using System.Drawing;
using UVOCBot.Responders.Census;

namespace UVOCBot
{
    public static class BotConstants
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

        public const string CENSUS_EVENTSTREAM_CLIENT_NAME = "main";

        public static readonly SubscribeCommand CORE_CENSUS_SUBSCRIPTION = new(
            new string[] { "all" },
            new string[] { EventNames.FACILITY_CONTROL },
            worlds: new string[] { "all" });
    }
}
