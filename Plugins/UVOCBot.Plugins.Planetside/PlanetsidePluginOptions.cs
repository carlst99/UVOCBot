namespace UVOCBot.Plugins.Planetside;

public class PlanetsidePluginOptions
{
    public const string CONFIG_KEY = "PlanetsidePluginOptions";

    /// <summary>
    /// Gets the endpoint at which the fisu API can be found.
    /// </summary>
    public string FisuApiEndpoint { get; init; } = "https://ps2.fisu.pw/api";

    /// <summary>
    /// Gets the endpoint at which the honu API can be found.
    /// </summary>
    public string HonuApiEndpoint { get; init; } = "https://wt.honu.pw/api";

    /// <summary>
    /// Whether to log failures to retrieve population data from Sanctuary.Census.
    /// </summary>
    public bool LogOnSanctuaryPopRetrievalFailures { get; init; } = true;
}
