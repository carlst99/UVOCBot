using System.Collections.Generic;

namespace UVOCBotRemora.Model.Planetside
{
    // https://census.daybreakgames.com/get/ps2/outfit?outfit_id=37570391403474619&c:show=name,outfit_id&c:join=outfit_member%5Einject_at:members%5Eshow:character_id%5Eouter:0%5Elist:1(character%5Eshow:name.first%5Einject_at:character%5Eouter:0%5Eon:character_id(characters_online_status%5Einject_at:online_status%5Eshow:online_status%5Eouter:0(world%5Eon:online_status%5Eto:world_id%5Eouter:0%5Eshow:world_id%5Einject_at:ignore_this))
    public class OnlineStatus
    {
        public class MemberModel
        {
            public class CharacterModel
            {
                public class OnlineStatusModel
                {
                    public bool OnlineStatus { get; set; }
                }

                public OnlineStatusModel OnlineStatus { get; set; }
            }

            public long CharacterId { get; set; }

        }

        public long OutfitId { get; set; }
        public string Name { get; set; }
        public List<MemberModel> Members { get; set; }
    }
}
