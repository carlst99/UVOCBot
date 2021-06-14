using System.Collections.Generic;

namespace UVOCBot.Core.Model
{
    public record GuildWelcomeMessageDto
    {
        /// <summary>
        /// Gets the roles to let the user alternatively assign to themselves.
        /// </summary>
        /// <remarks>Useful for separation between new members and people visiting from an existing outfit.</remarks>
        public IReadOnlyList<ulong> AlternateRoles { get; init; }

        /// <summary>
        /// Gets the roles to assign the user by default.
        /// </summary>
        public IReadOnlyList<ulong> DefaultRoles { get; init; }

        /// <summary>
        /// Gets a value indicating if an attempt will be made to provide nickname options, based off the last few in-game joins.
        /// </summary>
        public bool DoIngameNameGuess { get; init; }

        /// <summary>
        /// Gets the ID of the guild that this object is storing data for.
        /// </summary>
        public ulong GuildId { get; init; }

        /// <summary>
        /// Gets a value indicating if this feature is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the message to send when a new user joins the server.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Gets the ID of the outfit to use for making nickname guesses.
        /// </summary>
        public ulong OutfitId { get; init; }

        public GuildWelcomeMessageDto(ulong guildId)
        {
            AlternateRoles = new List<ulong>();
            DefaultRoles = new List<ulong>();
            GuildId = guildId;
            Message = "Welcome <name>!";
        }
    }
}
