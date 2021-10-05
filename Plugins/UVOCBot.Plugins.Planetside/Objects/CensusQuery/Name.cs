namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery
{
    /// <summary>
    /// Initialises a new instance of the <see cref="Name"/> record.
    /// </summary>
    /// <param name="First">The first name.</param>
    /// <param name="Last">The last name.</param>
    public record Name
    (
        string First,
        string Last
    );
}