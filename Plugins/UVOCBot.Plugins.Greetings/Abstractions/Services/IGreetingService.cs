using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.Greetings.Abstractions.Services;

/// <summary>
/// Represents an interface for helping with greeting tasks.
/// </summary>
public interface IGreetingService
{
    /// <summary>
    /// Sends a greeting to the given member.
    /// </summary>
    /// <param name="member">The guild member.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>The greeting message, if one was sent.</returns>
    Task<Result<IMessage?>> SendGreeting
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
        CancellationToken ct
    );
}
