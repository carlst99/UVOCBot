using static UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit.NewOutfitMember;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

/// <summary>
/// Initialises a new instance of the <see cref="NewOutfitMember"/> record.
/// The query model for https://census.daybreakgames.com/get/ps2/outfit_member?outfit_id=37562651025751157&c:sort=member_since:-1&c:show=character_id,member_since&c:join=character%5Eshow:name.first%5Einject_at:character_name&c:limit=10
/// </summary>
/// <param name="CharacterID">The ID of the character.</param>
/// <param name="MemberSince">The datetime that the character was created.</param>
/// <param name="CharacterName">The name of the character.</param>
public record NewOutfitMember
(
    ulong CharacterID,
    DateTimeOffset MemberSince,
    CharacterNameModel CharacterName
)
{
    public record CharacterNameModel
    (
        Name Name
    );
}
