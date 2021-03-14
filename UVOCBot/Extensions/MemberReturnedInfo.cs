using DSharpPlus.Entities;

namespace UVOCBot.Extensions
{
    public struct MemberReturnedInfo
    {
        public enum GetMemberStatus
        {
            /// <summary>
            /// The guild member was successfully retrieved
            /// </summary>
            Success = 0,

            /// <summary>
            /// The guild itself could not be found
            /// </summary>
            GuildNotFound = 1,

            /// <summary>
            /// The guild member could not be retrieved
            /// </summary>
            Failure = 2
        }

        public DiscordMember Member { get; }
        public GetMemberStatus Status { get; }

        public MemberReturnedInfo(DiscordMember member, GetMemberStatus status)
        {
            Member = member;
            Status = status;
        }
    }
}
