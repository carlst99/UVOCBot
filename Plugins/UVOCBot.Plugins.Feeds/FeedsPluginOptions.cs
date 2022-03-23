namespace UVOCBot.Plugins.Feeds;

public class FeedsPluginOptions
{
    public bool EnableTwitterFeed { get; init; }
    public string TwitterKey { get; init; }
    public string TwitterSecret { get; init; }
    public string TwitterBearerToken { get; init; }

    public FeedsPluginOptions()
    {
        EnableTwitterFeed = true;
        TwitterKey = string.Empty;
        TwitterSecret = string.Empty;
        TwitterBearerToken = string.Empty;
    }
}
