using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core.Model;

namespace UVOCBot.Plugins.Greetings.Abstractions.Services;

/// <summary>
/// Represents an interface for helping with greeting tasks.
/// </summary>
public interface IGreetingService
{
    /// <summary>
    /// Sends a greeting to the given member.
    /// </summary>
    /// <param name="guildID">The ID of the guild in which to send the greeting.</param>
    /// <param name="member">The guild member.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A <see cref="Result"/> representing the outcome of the operation.</returns>
    Task<Result> SendGreeting
    (
        Snowflake guildID,
        IGuildMember member,
        CancellationToken ct = default
    );

    /// <summary>
    /// Performs a fuzzy nickname guess using the character names of new outfit members.
    /// </summary>
    /// <param name="username">The Discord username.</param>
    /// <param name="outfitId">The PlanetSide 2 outfit id.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A list of potential nicknames.</returns>
    Task<Result<IEnumerable<string>>> DoFuzzyNicknameGuess
    (
        string username,
        ulong outfitId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Grants the alternate role set to a guild member.
    /// </summary>
    /// <param name="guildID">The ID of the guild in which the request originated.</param>
    /// <param name="member">The member to apply the alternate role set to.</param>
    /// <param name="rolesetID">The ID of the roleset.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A list of the role IDs that were applied.</returns>
    Task<Result<IReadOnlyList<ulong>>> SetAlternateRolesetAsync
    (
        Snowflake guildID,
        IGuildMember member,
        ulong rolesetID,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets an array of <see cref="ISelectOption"/>s that can be used to
    /// build a select menu for alternate rolesets.
    /// </summary>
    /// <param name="alternateRolesets">The alternate rolesets to include in the menu.</param>
    /// <returns>A <see cref="Result"/> representing the outcome of the operation, and the array of select options upon success.</returns>
    ISelectOption[] CreateAlternateRoleSelectOptions(IReadOnlyList<GuildGreetingAlternateRoleSet> alternateRolesets);
}
