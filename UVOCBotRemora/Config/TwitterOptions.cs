namespace UVOCBotRemora.Config
{
    public class TwitterOptions
    {
        public const string ConfigSectionName = "TwitterOptions";

#nullable disable

        public string Key { get; set; }
        public string Secret { get; set; }
        public string BearerToken { get; set; }

#nullable restore
    }
}
