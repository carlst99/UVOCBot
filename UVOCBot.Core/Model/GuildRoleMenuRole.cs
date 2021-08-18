using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model
{
    public class GuildRoleMenuRole
    {
        /// <summary>
        /// Gets or sets the ID of the guild that this role belongs to.
        /// </summary>
        [Key]
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the optional label of the role.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the optional description of the role.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the role to apply.
        /// </summary>
        public ulong RoleId { get; set; }

        /// <summary>
        /// Gets or sets the optional emoji to display.
        /// </summary>
        public string? Emoji { get; set; }

        public GuildRoleMenuRole(ulong roleId, string label)
        {
            RoleId = roleId;
            Label = label;
            Description = string.Empty;
        }
    }
}
