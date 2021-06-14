﻿using System.Collections.Generic;

namespace UVOCBot.Core.Model
{
    public record GuildWelcomeMessageDto
    {
        /// <summary>
        /// Gets or sets the ID of the guild that this object is storing data for.
        /// </summary>
        public ulong GuildId { get; init; }

        /// <summary>
        /// Gets or sets a value indicating if an attempt will be made to provide nickname options, based off the last few in-game joins.
        /// </summary>
        public bool DoIngameNameGuess { get; init; }

        /// <summary>
        /// Gets or sets a value indicating if this feature is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets or sets the message to send when a new user joins the server.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Gets or sets the roles to let the user alternatively assign to themselves.
        /// </summary>
        /// <remarks>Useful for separation between new members and people visiting from an existing outfit.</remarks>
        public IReadOnlyList<ulong> AlternateRoles { get; init; }

        /// <summary>
        /// Gets or sets the roles to assign the user by default.
        /// </summary>
        public IReadOnlyList<ulong> DefaultRoles { get; init; }

        public GuildWelcomeMessageDto()
        {
            Message = "Welcome <name>!";
            AlternateRoles = new List<ulong>();
            DefaultRoles = new List<ulong>();
        }
    }
}
