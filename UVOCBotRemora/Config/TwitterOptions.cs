namespace UVOCBotRemora.Config
{
    public class TwitterOptions
    {
        public const string ConfigSectionName = "TwitterOptions";

#nullable disable

        public string Key { get; init; }
        public string Secret { get; init; }
        public string BearerToken { get; init; }

#nullable restore
    }
}
