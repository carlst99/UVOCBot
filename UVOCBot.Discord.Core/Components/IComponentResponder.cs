using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Discord.Core.Components;

/// <summary>
/// Represents a responder for component interactions.
/// </summary>
public interface IComponentResponder
{
    /// <summary>
    /// Gets the response attributes to use for the given key.
    /// For example, this could include the <see cref="Remora.Discord.Commands.Attributes.EphemeralAttribute"/>
    /// </summary>
    /// <param name="key">The component key to retrieve attributes for.</param>
    /// <returns>The attributes to use when creating a response for this component key.</returns>
    Result<Attribute[]> GetResponseAttributes(string key);

    /// <summary>
    /// Runs the responder logic.
    /// </summary>
    /// <param name="key">The component key.</param>
    /// <param name="dataFragment">The optional data included in the component interaction.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default);
}
