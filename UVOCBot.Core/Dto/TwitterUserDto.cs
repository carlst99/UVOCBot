using System.Collections.Generic;

namespace UVOCBot.Core.Dto;

public sealed class TwitterUserDto
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

    public TwitterUserDto() { }

    public TwitterUserDto(long id)
    {
        UserId = id;
        LastRelayedTweetId = null;
    }

    public override bool Equals(object? obj)
        => obj is TwitterUserDto user
            && user.UserId.Equals(UserId);

    public override int GetHashCode() => UserId.GetHashCode();
}
