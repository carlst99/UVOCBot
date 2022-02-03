using Remora.Discord.API;
using System.Diagnostics.CodeAnalysis;

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

    /// <inheritdoc cref="Snowflake.TryParse(string, out Snowflake?, ulong)"/>
    public static bool TryParse(string value, [NotNullWhen(true)] out Snowflake? result)
        => Snowflake.TryParse(value, out result, Constants.DiscordEpoch);
}
