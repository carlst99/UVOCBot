using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Greetings.Objects;

namespace UVOCBot.Plugins.Greetings.Abstractions.Services;

/// <summary>
/// Represents an interface for performing Census queries.
/// </summary>
public interface ICensusQueryService
{
    /// <summary>
    /// Gets new members of an outfit.
    /// </summary>
    /// <param name="outfitId">The ID of the outfit.</param>
    /// <param name="limit">The number of new members to get.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A list of the new outfit members.</returns>
    Task<Result<IReadOnlyList<NewOutfitMember>>> GetNewOutfitMembersAsync(ulong outfitId, uint limit, CancellationToken ct = default);
}
