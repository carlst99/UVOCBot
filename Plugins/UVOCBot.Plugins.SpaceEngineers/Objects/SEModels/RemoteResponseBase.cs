namespace UVOCBot.Plugins.SpaceEngineers.Objects.SEModels;

public record RemoteResponseBase<TResponse>
(
    TResponse Data,
    ResponseMetadata Meta
);
