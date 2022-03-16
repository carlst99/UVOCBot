using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects;

/// <summary>
/// Enumerates Census worlds that are valid for Census REST queries.
/// </summary>
public enum ValidWorldDefinition
{
    Connery = WorldDefinition.Connery,
    Miller = WorldDefinition.Miller,
    Cobalt = WorldDefinition.Cobalt,
    Emerald = WorldDefinition.Emerald,
    Jaeger = WorldDefinition.Jaeger,
    Soltech = WorldDefinition.Soltech
}
