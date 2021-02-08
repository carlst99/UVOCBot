namespace UVOCBot.Config
{
    public class GeneralOptions
    {
        public const string ConfigSectionName = "GeneralOptions";

        public string BotToken { get; set; }
        public string ApiEndpoint { get; set; }
        public string CensusApiKey { get; set; }
    }
}
