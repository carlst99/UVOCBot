using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core;

public class DatabaseOptions
{
    public const string CONFIG_NAME = "DatabaseOptions";

    /// <summary>
    /// The string used to initiate the database connection
    /// </summary>
    [Required]
    public string ConnectionString { get; init; } = null!;
}
