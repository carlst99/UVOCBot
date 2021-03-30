namespace UVOCBotRemora.Config
{
    public class GeneralOptions
    {
        public const string ConfigSectionName = "GeneralOptions";

#nullable disable

        public string BotToken { get; set; }
        public string ApiEndpoint { get; set; }
        public string FisuApiEndpoint { get; set; }
        public string CensusApiKey { get; set; }
        public string CommandPrefix { get; set; }

#nullable restore
    }
}
