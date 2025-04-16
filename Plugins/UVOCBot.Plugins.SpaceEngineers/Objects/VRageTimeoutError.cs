using Remora.Results;

namespace UVOCBot.Plugins.SpaceEngineers.Objects;

public record VRageTimeoutError : ResultError
{
    public VRageTimeoutError()
        : base("The server is offline")
    {
    }
}
