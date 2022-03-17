namespace UVOCBot.Plugins.Planetside;

public record PlanetsidePluginOptions
{
    /// <summary>
    /// Gets the endpoint at which the fisu API can be found.
    /// </summary>
    public string FisuApiEndpoint { get; init; }

    /// <summary>
    /// Gets the endpoint at which the honu API can be found.
    /// </summary>
    public string HonuApiEndpoint { get; init; }

    public PlanetsidePluginOptions()
    {
        FisuApiEndpoint = "https://ps2.fisu.pw/api";
        HonuApiEndpoint = "https://wt.honu.pw/api";
    }
}
