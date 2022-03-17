using System.Collections.Generic;

namespace UVOCBot.Config;

public record GeneralOptions
{
    /// <summary>
    /// Gets or sets the Discord bot token.
    /// </summary>
    public string BotToken { get; init; }

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
        BotToken = string.Empty;
        DebugGuildIds = new List<ulong>();
        DiscordPresence = string.Empty;
    }
}
