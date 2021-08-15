using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model
{
    public interface IGuildObject
    {
        [Key]
        ulong GuildId { get; set; }
    }
}
