namespace UVOCBot.Core;

public class DatabaseOptions
{
    public const string ConfigSectionName = "DatabaseOptions";

    /// <summary>
    /// The string used to initiate the database connection
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}
