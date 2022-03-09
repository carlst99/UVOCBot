using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;

namespace UVOCBot.Discord.Core.Services;

/// <inheritdoc cref="IInteractionResponseService"/>
public class InteractionResponseService : IInteractionResponseService
{
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly InteractionContext _context;

    /// <inheritdoc />
    public bool HasResponded { get; protected set; }

    /// <inheritdoc />
    public bool WillDefaultToEphemeral { get; set; }

    public InteractionResponseService
    (
        IDiscordRestInteractionAPI interactionApi,
        InteractionContext context
    )
    {
        _interactionApi = interactionApi;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> CreateModalResponse
    (
        InteractionModalCallbackData modalData,
        CancellationToken ct = default
    )
    {
        if (HasResponded)
            return new InvalidOperationError("A response has already been created for this scope.");

        Result responseResult = await _interactionApi.CreateInteractionResponseAsync
        (
            _context.ID,
            _context.Token,
            new InteractionResponse
            (
                InteractionCallbackType.Modal,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(modalData)
            ),
            ct: ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }

    /// <inheritdoc />
    public async Task<Result> CreateMessageResponse
    (
        InteractionMessageCallbackData message,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
        CancellationToken ct = default
    )
    {
        if (HasResponded)
            return new InvalidOperationError("A response has already been created for this scope.");

        if (WillDefaultToEphemeral && !message.Flags.HasValue)
            message = message with { Flags = MessageFlags.Ephemeral };

        Result responseResult = await _interactionApi.CreateInteractionResponseAsync
        (
            _context.ID,
            _context.Token,
            new InteractionResponse
            (
                InteractionCallbackType.ChannelMessageWithSource,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(message)
            ),
            attachments,
            ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }

    /// <inheritdoc />
    public async Task<Result> CreateDeferredMessageResponse(CancellationToken ct = default)
    {
        if (HasResponded)
            return new InvalidOperationError("A response has already been created for this scope.");

        Optional<MessageFlags> flags = WillDefaultToEphemeral
            ? MessageFlags.Ephemeral
            : default;

        Result responseResult = await _interactionApi.CreateInteractionResponseAsync
        (
            _context.ID,
            _context.Token,
            new InteractionResponse
            (
                InteractionCallbackType.DeferredChannelMessageWithSource,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>
                (
                    new InteractionMessageCallbackData(Flags: flags)
                )
            ),
            ct: ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }

    /// <inheritdoc />
    public async Task<Result> CreateContextualMessageResponse
    (
        Optional<string> content = default,
        Optional<bool> isTTS = default,
        Optional<IReadOnlyList<IEmbed>> embeds = default,
        Optional<IAllowedMentions> allowedMentions = default,
        Optional<IReadOnlyList<IMessageComponent>> components = default,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
        Optional<MessageFlags> flags = default,
        CancellationToken ct = default
    )
    {
        if (WillDefaultToEphemeral && !flags.HasValue)
            flags = MessageFlags.Ephemeral;

        if (!HasResponded)
        {
            InteractionMessageCallbackData messageData = new
            (
                isTTS,
                content,
                embeds,
                allowedMentions,
                flags,
                components
            );

            return await CreateMessageResponse(messageData, attachments, ct);
        }

        Result<IMessage> responseResult = await _interactionApi.CreateFollowupMessageAsync
        (
            DiscordConstants.ApplicationId,
            _context.Token,
            content,
            isTTS,
            embeds,
            allowedMentions,
            components,
            attachments,
            flags,
            ct
        );

        return responseResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(responseResult);
    }
}
