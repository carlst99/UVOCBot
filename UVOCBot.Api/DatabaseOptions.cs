namespace UVOCBot.Api
{
    public class DatabaseOptions
    {
        public const string ConfigSectionName = "DatabaseOptions";

        /// <summary>
        /// The string used to initiate the database connection
        /// </summary>
        public string ConnectionString { get; init; }

        /// <summary>
        /// The version of MariaDB which is being connected to
        /// </summary>
        public string DatabaseVersion { get; init; }

        public DatabaseOptions()
        {
            ConnectionString = string.Empty;
            DatabaseVersion = string.Empty;
        }
    }
}
