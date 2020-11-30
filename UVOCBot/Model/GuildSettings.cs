using Realms;

namespace UVOCBot.Model
{
    /// <summary>
    /// Contains settings pertinent to a guild's preferences
    /// </summary>
    public sealed class GuildSettings : RealmObject
    {
        [PrimaryKey]
        public int Id { get; set; }
    }
}
