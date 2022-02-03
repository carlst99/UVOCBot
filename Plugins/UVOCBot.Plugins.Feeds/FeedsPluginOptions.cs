namespace UVOCBot.Plugins.Feeds;

public record FeedsPluginOptions
{
    public string TwitterKey { get; init; }
    public string TwitterSecret { get; init; }
    public string TwitterBearerToken { get; init; }

    public FeedsPluginOptions()
    {
        TwitterKey = string.Empty;
        TwitterSecret = string.Empty;
        TwitterBearerToken = string.Empty;
    }
}
