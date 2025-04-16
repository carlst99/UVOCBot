using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

public record ExperienceRank
(
    uint Id,
    int Rank,
    int PrestigeLevel,
    decimal XpMax,
    ExperienceRank.FactionInfo VS,
    string? VsImagePath,
    ExperienceRank.FactionInfo NC,
    string? NcImagePath,
    ExperienceRank.FactionInfo TR,
    string? TrImagePath,
    ExperienceRank.FactionInfo NSO,
    string? NsoImagePath
)
{
    /// <summary>
    /// Represents faction-specific information about an <see cref="ExperienceRank"/>.
    /// </summary>
    /// <param name="Title">The title of the rank.</param>
    /// <param name="ImageSetId">The ID of the rank's image set.</param>
    /// <param name="ImageId">The ID of the rank's image.</param>
    public record FactionInfo
    (
        GlobalizedString? Title,
        uint? ImageSetId,
        uint? ImageId
    );
}

