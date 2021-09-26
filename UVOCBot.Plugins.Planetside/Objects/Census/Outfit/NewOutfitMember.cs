namespace UVOCBot.Plugins.Planetside.Objects.Census.Outfit
{
    /// <summary>
    /// The query model for https://census.daybreakgames.com/get/ps2/outfit_member?outfit_id=37562651025751157&c:sort=member_since:-1&c:show=character_id,member_since&c:join=character%5Eshow:name.first%5Einject_at:character_name&c:limit=10
    /// </summary>
    public record NewOutfitMember
    {
        public record CharacterNameModel
        {
            public Name Name { get; init; }

            public CharacterNameModel()
            {
                Name = new Name();
            }
        }

        /// <summary>
        /// The ID of the character.
        /// </summary>
        public ulong CharacterId { get; init; }

        /// <summary>
        /// The time that this member joined the outfit.
        /// </summary>
        public DateTimeOffset MemberSince { get; init; }

        public CharacterNameModel CharacterName { get; init; }

        public NewOutfitMember()
        {
            CharacterName = new CharacterNameModel();
        }
    }
}
