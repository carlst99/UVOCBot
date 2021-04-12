using System.Collections.Generic;

namespace UVOCBot.Model.Planetside
{
#nullable disable
    // https://census.daybreakgames.com/get/ps2/outfit?outfit_id=37570391403474619&c:show=name,outfit_id&c:join=outfit_member%5Einject_at:members%5Eshow:character_id%5Eouter:0%5Elist:1(character%5Eshow:name.first%5Einject_at:character%5Eouter:0%5Eon:character_id(characters_online_status%5Einject_at:online_status%5Eshow:online_status%5Eouter:0(world%5Eon:online_status%5Eto:world_id%5Eouter:0%5Eshow:world_id%5Einject_at:ignore_this))
    public record OnlineStatus
    {
        public record MemberModel
        {
            public record CharacterModel
            {
                public record OnlineStatusModel
                {
                    public bool OnlineStatus { get; init; }
                }

                public OnlineStatusModel OnlineStatus { get; init; }
            }

            public long CharacterId { get; init; }
        }

        public long OutfitId { get; init; }
        public string Name { get; init; }
        public List<MemberModel> Members { get; init; }
    }
#nullable restore
}
