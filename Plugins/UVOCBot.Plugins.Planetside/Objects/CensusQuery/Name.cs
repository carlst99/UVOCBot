namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

/// <summary>
/// Initialises a new instance of the <see cref="Name"/> record.
/// </summary>
/// <param name="First">The first name.</param>
/// <param name="FirstLower">A lower-case version of <paramref name="First"/>.</param>
public record Name
(
    string First,
    string FirstLower
);
