using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
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
    /// <param name="modalData">The modal to respond with.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateModalResponse
    (
        InteractionModalCallbackData modalData,
        CancellationToken ct = default
    );

    /// <summary>
    /// Creates a message interaction response.
    /// </summary>
    /// <param name="message">The message to response with.</param>
    /// <param name="attachments">The attachments to send with the message.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateMessageResponse
    (
        InteractionMessageCallbackData message,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
        CancellationToken ct = default
    );

    /// <summary>
    /// Creates a deferred message response. Use <see cref="CreateContextualMessageResponse"/>
    /// or similar means to send followup messages.
    /// </summary>
    /// <param name="isEphemeral">A value indicating whether the deferred response should be ephemeral.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateDeferredMessageResponse
    (
        bool isEphemeral,
        CancellationToken ct = default
    );

    /// <summary>
    /// Creates a message interaction response. If an interaction response has already been
    /// created for the scoped token, a followup message will be sent instead.
    /// </summary>
    /// <param name="message">The message to response with.</param>
    /// <param name="attachments">The attachments to send with the message.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateContextualMessageResponse
    (
        InteractionMessageCallbackData message,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
        CancellationToken ct = default
    );
}
