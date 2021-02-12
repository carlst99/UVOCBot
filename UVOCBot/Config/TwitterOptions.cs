namespace UVOCBot.Config
{
    public class TwitterOptions
    {
        public const string ConfigSectionName = "TwitterOptions";

        public string Key { get; set; }
        public string Secret { get; set; }
        public string BearerToken { get; set; }
    }
}
