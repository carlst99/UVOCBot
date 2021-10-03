using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model
{
    public class GuildAdminSettings : IGuildObject
    {
        /// <inheritdoc />
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if admin logging is enabled.
        /// </summary>
        public bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the ID of the channel to which admin logs will be made.
        /// </summary>
        public ulong? LoggingChannelId { get; set; }

        /// <summary>
        /// Gets or sets the type of logs to make. This is intended to be a bitwise enum field.
        /// </summary>
        public ulong LogTypes { get; set; }

        /// <summary>
        /// Do not use this constructor unless you going to set <see cref="GuildId"/> immediately after construction.
        /// </summary>
        public GuildAdminSettings()
        {
        }

        public GuildAdminSettings(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}
