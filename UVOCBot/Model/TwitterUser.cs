namespace UVOCBot.Model
{
    public class TwitterUser
    {
        /// <summary>
        /// The twitter ID of this user
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// The last tweet that was relayed from this user
        /// </summary>
        public long? LastRelayedTweetId { get; set; }

        public TwitterUser() { }

        public TwitterUser(long id) => UserId = id;

        public override bool Equals(object obj) => obj is TwitterUser user
                && user.UserId.Equals(UserId);

        public override int GetHashCode() => UserId.GetHashCode();
    }
}
