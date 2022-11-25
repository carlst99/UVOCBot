namespace UVOCBot.Plugins.ApexLegends.Objects;

public class ApexPluginOptions
{
    /// <summary>
    /// Gets the endpoint at which the Apex Legends API can be found.
    /// </summary>
    public string ApexLegendsApiEndpoint { get; init; }

    /// <summary>
    /// Gets the key used to authorize with the Apex Legends API.
    /// </summary>
    public string ApexLegendsApiKey { get; init; }

    public ApexPluginOptions()
    {
        ApexLegendsApiEndpoint = "https://api.mozambiquehe.re";
        ApexLegendsApiKey = string.Empty;
    }
}
