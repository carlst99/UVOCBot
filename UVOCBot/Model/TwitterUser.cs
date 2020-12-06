using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Model
{
    public class TwitterUser
    {
        [Key]
        public long UserId { get; set; }

        public ICollection<GuildTwitterSettings> Guilds { get; } = new List<GuildTwitterSettings>();

        public TwitterUser() { }

        public TwitterUser(long id) => UserId = id;
    }
}
