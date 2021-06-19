using System.Collections.Generic;

namespace UVOCBot.Config
{
    public record GeneralOptions
    {
        /// <summary>
        /// Gets or sets the Discord bot token.
        /// </summary>
        public string BotToken { get; init; }

        /// <summary>
        /// Gets or sets the endpoint at which the data layer API can be found.
        /// </summary>
        public string ApiEndpoint { get; init; }

        /// <summary>
        /// Gets or sets the endpoint at which the fisu's API can be found.
        /// </summary>
        public string FisuApiEndpoint { get; init; }

        /// <summary>
        /// Gets or sets the key used to connect to Daybreak Game's Census API.
        /// </summary>
        public string CensusApiKey { get; init; }

        /// <summary>
        /// Gets or sets the default command prefix.
        /// </summary>
        public string CommandPrefix { get; init; }

        /// <summary>
        /// The text to show in the custom status area of the bot profile. Will always be prefixed by the 'Playing' operator.
        /// </summary>
        public string DiscordPresence { get; init; }

        /// <summary>
        /// Gets or sets the ID of the guilds used for debugging slash commands.
        /// </summary>
        public List<ulong> DebugGuildIds { get; init; }

        public GeneralOptions()
        {
            ApiEndpoint = string.Empty;
            BotToken = string.Empty;
            CensusApiKey = string.Empty;
            CommandPrefix = "<>"; // No prefix, most commands use the slash infrastructure
            DebugGuildIds = new List<ulong>();
            DiscordPresence = string.Empty;
            FisuApiEndpoint = string.Empty;
        }
    }
}
