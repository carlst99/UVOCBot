using DSharpPlus.Entities;

namespace UVOCBot.Extensions
{
    public struct ChannelReturnedInfo
    {
        public enum GetChannelStatus
        {
            /// <summary>
            /// The channel was found successfully
            /// </summary>
            Success = 0,

            /// <summary>
            /// The specified channel could not be found, but a fallback channel was utilised
            /// </summary>
            Fallback = 1,

            /// <summary>
            /// The guild itself could not be found
            /// </summary>
            GuildNotFound = 2,

            /// <summary>
            /// Fetching the channel failed
            /// </summary>
            Failure = 3
        }

        public DiscordChannel Channel { get; }
        public GetChannelStatus Status { get; }

        public ChannelReturnedInfo(DiscordChannel channel, GetChannelStatus status)
        {
            Channel = channel;
            Status = status;
        }
    }
}
