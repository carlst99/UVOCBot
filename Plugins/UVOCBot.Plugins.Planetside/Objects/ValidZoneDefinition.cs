using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects;

/// <summary>
/// Enumerates Census zones that are valid for Census REST queries.
/// </summary>
public enum ValidZoneDefinition : ushort
{
    Indar = ZoneDefinition.Indar,
    Hossin = ZoneDefinition.Hossin,
    Amerish = ZoneDefinition.Amerish,
    Esamir = ZoneDefinition.Esamir,
    Oshur = ZoneDefinition.Oshur
}
