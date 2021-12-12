using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Discord.Core.Components;

/// <summary>
/// Represents a responder for component interactions.
/// </summary>
public interface IComponentResponder
{
    /// <summary>
    /// Runs the responder logic.
    /// </summary>
    /// <param name="key">The component key.</param>
    /// <param name="dataFragment">The optional data included in the component interaction.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> RespondAsync(string key, string? dataFragment, CancellationToken ct = default);
}
