using Remora.Discord.API;

namespace Remora.Rest.Core;

public static class DiscordSnowflake
{
    /// <summary>
    /// Initialzes a new instance of a Snowflake with the Discord epoch.
    /// </summary>
    /// <param name="value">The snowflake value.</param>
    /// <returns>A snowflake.</returns>
    public static Snowflake New(ulong value)
        => new(value, Constants.DiscordEpoch);
}
