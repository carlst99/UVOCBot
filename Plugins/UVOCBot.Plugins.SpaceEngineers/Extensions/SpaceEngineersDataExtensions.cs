using UVOCBot.Core.Model;
using UVOCBot.Plugins.SpaceEngineers.Objects;

namespace UVOCBot.Plugins.SpaceEngineers.Extensions;

public static class SpaceEngineersDataExtensions
{
    public static bool TryGetConnectionDetails
    (
        this SpaceEngineersData data,
        out SEServerConnectionDetails connectionDetails
    )
    {
        connectionDetails = default;

        bool missingConnectionDetails = string.IsNullOrEmpty(data.ServerAddress)
            || data.ServerPort is not > 0
            || string.IsNullOrEmpty(data.ServerKey);

        if (missingConnectionDetails)
            return false;

        connectionDetails = new SEServerConnectionDetails
        (
            data.ServerAddress!,
            data.ServerPort!.Value,
            data.ServerKey!
        );

        return true;
    }
}
