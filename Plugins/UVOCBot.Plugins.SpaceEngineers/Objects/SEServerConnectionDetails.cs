namespace UVOCBot.Plugins.SpaceEngineers.Objects;

public readonly record struct SEServerConnectionDetails
(
    string Address,
    int Port,
    string Key
);
