using Remora.Discord.API.Objects;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Discord.Core.Abstractions.Services;

/// <summary>
/// Represents a service for responding to interactions.
/// </summary>
public interface IInteractionResponseService
{
    /// <summary>
    /// Gets a value indicating whether or not an interaction response has been made for the current scope.
    /// </summary>
    public bool HasResponded { get; }

    /// <summary>
    /// Creates a modal interaction response.
    /// </summary>
    /// <param name="modalData">The modal to submit.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateModalResponse(InteractionModalCallbackData modalData, CancellationToken ct = default);
}
