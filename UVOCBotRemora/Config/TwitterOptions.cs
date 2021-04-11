namespace UVOCBotRemora.Config
{
    public record TwitterOptions
    {
        public const string ConfigSectionName = "TwitterOptions";

        public string Key { get; init; }
        public string Secret { get; init; }
        public string BearerToken { get; init; }

        public TwitterOptions()
        {
            Key = string.Empty;
            Secret = string.Empty;
            BearerToken = string.Empty;
        }
    }
}
