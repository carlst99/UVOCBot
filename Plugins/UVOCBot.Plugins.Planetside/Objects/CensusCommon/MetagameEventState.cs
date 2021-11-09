namespace UVOCBot.Plugins.Planetside.Objects.CensusCommon;

/// <summary>
/// Enumerates the various states a <see cref="MetagameEvent"/> can represent.
/// </summary>
public enum MetagameEventState : uint
{
    Started = 135,
    Restarted = 136,
    Canceled = 137,
    Ended = 138,
    XPBonusChanged = 139
}
