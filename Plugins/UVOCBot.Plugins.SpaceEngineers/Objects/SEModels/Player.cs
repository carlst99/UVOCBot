namespace UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

public record Player
(
    long SteamID,
    string? DisplayName,
    string? FactionTag,
    string? FactionName,
    int PromoteLevel
);
