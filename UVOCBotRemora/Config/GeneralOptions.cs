using System.Collections.Generic;

namespace UVOCBotRemora.Config
{
    public class GeneralOptions
    {
        public const string ConfigSectionName = "GeneralOptions";

#nullable disable

        /// <summary>
        /// Gets or sets the Discord bot token
        /// </summary>
        public string BotToken { get; init; }

        /// <summary>
        /// Gets or sets the client ID of the Discord application that the bot is part of
        /// </summary>
        public ulong DiscordApplicationClientId { get; init; }

        /// <summary>
        /// Gets or sets the endpoint at which the data layer API can be found
        /// </summary>
        public string ApiEndpoint { get; init; }

        /// <summary>
        /// Gets or sets the endpoint at which the fisu's API can be found
        /// </summary>
        public string FisuApiEndpoint { get; init; }

        /// <summary>
        /// Gets or sets the key used to connect to Daybreak Game's Census API
        /// </summary>
        public string CensusApiKey { get; init; }

        /// <summary>
        /// Gets or sets the default command prefix
        /// </summary>
        public string CommandPrefix { get; init; }

        /// <summary>
        /// Gets or sets the ID of the guilds used for debugging slash commands
        /// </summary>
        public List<ulong> DebugGuildIds { get; init; }

#nullable restore
    }
}
