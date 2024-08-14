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
    /// Gets a value indicating whether an interaction response has been made for the current scope.
    /// </summary>
    public bool HasResponded { get; }

    /// <summary>
    /// Gets or sets a value indicating whether an
    /// interaction response should default to being ephemeral.
    /// </summary>
    public bool WillDefaultToEphemeral { get; set; }

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
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateDeferredMessageResponse(CancellationToken ct = default);

    /// <summary>
    /// Creates a message interaction response. If an interaction response has already been
    /// created for the scoped token, a followup message will be sent instead.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="isTTS">Whether this message is a TTS message.</param>
    /// <param name="embeds">The embeds in the message.</param>
    /// <param name="allowedMentions">The set of allowed mentions of the message.</param>
    /// <param name="components">The components that should be included with the message.</param>
    /// <param name="attachments">
    /// The attachments to associate with the response. Each file may be a new file in the form of
    /// <see cref="FileData"/>, or an existing one that should be retained in the form of a
    /// <see cref="IPartialAttachment"/>. If this request edits the original message, then any attachments not
    /// mentioned in this parameter will be deleted.
    /// </param>
    /// <param name="flags">The message flags to use.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> CreateContextualMessageResponse
    (
        Optional<string> content = default,
        Optional<bool> isTTS = default,
        Optional<IReadOnlyList<IEmbed>> embeds = default,
        Optional<IAllowedMentions> allowedMentions = default,
        Optional<IReadOnlyList<IMessageComponent>> components = default,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
        Optional<MessageFlags> flags = default,
        CancellationToken ct = default
    );
}
