namespace UVOCBot.Api
{
    public class DatabaseOptions
    {
        public const string ConfigSectionName = "DatabaseOptions";

        public string ConnectionString { get; set; }
        public string DatabaseVersion { get; set; }
    }
}
