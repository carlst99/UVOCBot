using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

public record CharactersFriends
(
    ulong CharacterID,
    Name Name,
    CharactersFriends.FriendsCollection Friends
)
{
    public record FriendsCollection(IReadOnlyList<Friend> FriendList);

    public record Friend
    (
        ulong CharacterID,
        DateTimeOffset LastLoginTime,
        [property: JsonPropertyName("online")]
        bool IsOnline,
        FriendName Name
    );

    public record FriendName(Name Name);
}
