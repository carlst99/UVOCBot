using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model
{
    public interface IGuildObject
    {
        /// <summary>
        /// Gets or sets the ID of the guild that this entry belongs to.
        /// </summary>
        [Key]
        ulong GuildId { get; set; }
    }
}
