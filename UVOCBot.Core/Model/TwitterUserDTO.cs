using System.Collections.Generic;

namespace UVOCBot.Core.Model
{
    public sealed class TwitterUserDTO
    {
        /// <summary>
        /// The twitter ID of this user
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// The last tweet that was relayed from this user
        /// </summary>
        public long? LastRelayedTweetId { get; set; }

        /// <summary>
        /// Guilds that are relaying tweets from this user
        /// </summary>
        public IReadOnlyList<ulong> Guilds { get; set; } = new List<ulong>().AsReadOnly();

        public TwitterUserDTO() { }

        public TwitterUserDTO(long id)
        {
            UserId = id;
            LastRelayedTweetId = null;
        }

        public override bool Equals(object? obj)
            => obj is TwitterUserDTO user
                && user.UserId.Equals(UserId);

        public override int GetHashCode() => UserId.GetHashCode();
    }
}
