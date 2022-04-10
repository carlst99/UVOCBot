using System.Drawing;
using UVOCBot.Discord.Core;

namespace DbgCensus.Core.Objects;

public static class FactionDefinitionExtensions
{
    public static Color ToColor(this FactionDefinition factionDefinition)
        => factionDefinition switch
        {
            FactionDefinition.NC => Color.DodgerBlue,
            FactionDefinition.TR => Color.DarkRed,
            FactionDefinition.VS => Color.Purple,
            FactionDefinition.NSO => Color.LightGray,
            _ => DiscordConstants.DEFAULT_EMBED_COLOUR
        };
}
