using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Model
{
    /// <summary>
    /// Contains settings for the welcome message feature
    /// </summary>
    public class GuildWelcomeMessage
    {
        private const char ROLE_SPLIT_CHAR = ';';

        /// <summary>
        /// Gets or sets the ID of the guild that this object is storing data for.
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the label that is shown on the button to assign the alternate roles.
        /// </summary>
        public string AlternateRoleLabel { get; set; }

        /// <summary>
        /// Gets or sets the channel in which to send the welcome message.
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if an attempt will be made to provide nickname options, based off the last few in-game joins.
        /// </summary>
        public bool DoIngameNameGuess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this feature is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the message to send when a new user joins the server.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the roles to let the user alternatively assign to themselves.
        /// </summary>
        /// <remarks>Useful for separation between new members and people visiting from an existing outfit.</remarks>
        public string SerialisedAlternateRoles { get; set; }

        /// <summary>
        /// Gets or sets the roles to assign the user by default.
        /// </summary>
        public string SerialisedDefaultRoles { get; set; }

        /// <summary>
        /// Gets or sets the tag to use for making nickname guesses.
        /// </summary>
        public ulong OutfitId { get; set; }

        public GuildWelcomeMessage(ulong guildId)
        {
            AlternateRoleLabel = string.Empty;
            GuildId = guildId;
            Message = "Welcome <name>!";
            SerialisedAlternateRoles = string.Empty;
            SerialisedDefaultRoles = string.Empty;
        }

        public GuildWelcomeMessageDto ToDto()
            => new(GuildId)
            {
                AlternateRoles = SerialisedRolesToList(SerialisedAlternateRoles),
                DefaultRoles = SerialisedRolesToList(SerialisedDefaultRoles),
                DoIngameNameGuess = DoIngameNameGuess,
                IsEnabled = IsEnabled,
                Message = Message,
                OutfitId = OutfitId
            };

        public static GuildWelcomeMessage FromDto(GuildWelcomeMessageDto dto)
            => new(dto.GuildId)
            {
                DoIngameNameGuess = dto.DoIngameNameGuess,
                IsEnabled = dto.IsEnabled,
                Message = dto.Message,
                SerialisedAlternateRoles = SerialiseRolesList(dto.AlternateRoles),
                SerialisedDefaultRoles = SerialiseRolesList(dto.DefaultRoles),
                OutfitId = dto.OutfitId
            };

        private static IReadOnlyList<ulong> SerialisedRolesToList(string serialisedRoles)
        {
            List<ulong> roles = new();

            foreach (string value in serialisedRoles.Split(ROLE_SPLIT_CHAR))
                roles.Add(ulong.Parse(value));

            return roles.AsReadOnly();
        }

        private static string SerialiseRolesList(IEnumerable<ulong> roles) => string.Join(ROLE_SPLIT_CHAR, roles);
    }
}
