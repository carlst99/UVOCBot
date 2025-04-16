using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects;

/// <summary>
/// Enumerates Census worlds that are valid for Census REST queries.
/// </summary>
public enum ValidWorldDefinition
{
    Osprey = WorldDefinition.Connery,
    Miller = WorldDefinition.Miller,
    Jaeger = WorldDefinition.Jaeger,
    Soltech = WorldDefinition.Soltech
}
