namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

/// <summary>
/// Represents an outfit wars registration.
/// </summary>
/// <param name="OutfitID">The ID of the outfit.</param>
/// <param name="FactionID">The ID of the outfit's faction.</param>
/// <param name="WorldID">The world that the outfit is registered on.</param>
/// <param name="RegistrationOrder">The order in which the outfit was registered.</param>
/// <param name="MemberSignupCount">The number of members who have signed up for the war.</param>
public record OutfitWarRegistration
(
    ulong OutfitID,
    uint FactionID,
    uint WorldID,
    uint RegistrationOrder,
    uint MemberSignupCount
);
