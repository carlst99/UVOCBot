namespace UVOCBot.Api
{
    public class DatabaseConfig
    {
        public const string ConfigSectionName = "DatabaseConfig";

        public string ConnectionString { get; set; }
        public string DatabaseVersion { get; set; }
    }
}
