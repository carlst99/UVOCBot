using Remora.Discord.API.Abstractions.Objects;
using System.Collections.Generic;

namespace UVOCBot.Model
{
    public class AccountDistributionRequest
    {
        public const int DEFAULT_TIMEOUT_MIN = 2;

        /// <summary>
        /// The guild member that made the request
        /// </summary>
        public IGuildMember Requester { get; set; }

        /// <summary>
        /// The channel in which the request was made
        /// </summary>
        public IChannel RequestChannel { get; set; }

        /// <summary>
        /// The users to distribute accounts to
        /// </summary>
        public List<IGuildMember> Receivers { get; set; }

        public AccountDistributionRequest(IGuildMember requester, IChannel requestChannel, List<IGuildMember> receivers)
        {
            Requester = requester;
            RequestChannel = requestChannel;
            Receivers = receivers;
        }

        public override bool Equals(object? obj)
        {
            return obj is AccountDistributionRequest adr
                && adr.Requester.Equals(Requester);
        }

        public override int GetHashCode() => Requester.GetHashCode();
    }
}
