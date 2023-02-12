//
//  FeedbackService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Themes;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
using Remora.Results;
using UVOCBot.Discord.Core.Abstractions.Services;

namespace UVOCBot.Discord.Core.Commands;

/// <summary>
/// Handles sending formatted messages to users.
/// </summary>
public class FeedbackService
{
    private readonly ContextInjectionService _contextInjection;
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestUserAPI _userAPI;
    private readonly IInteractionResponseService _interactionResponseService;

    /// <summary>
    /// Gets the theme used by the feedback service.
    /// </summary>
    public IFeedbackTheme Theme { get; }

    /// <summary>
    /// Gets a value indicating whether the service, in the context of an interaction, has edited the original
    /// message.
    /// </summary>
    /// <remarks>This method always returns false in a message context.</remarks>
    public bool HasEditedOriginalMessage { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackService"/> class.
    /// </summary>
    /// <param name="channelAPI">The channel API.</param>
    /// <param name="userAPI">The user API.</param>
    /// <param name="contextInjection">The context injection service.</param>
    /// <param name="interactionResponseService">The interaction response service.</param>
    /// <param name="feedbackTheme">The feedback theme to use.</param>
    public FeedbackService
    (
        IDiscordRestChannelAPI channelAPI,
        IDiscordRestUserAPI userAPI,
        IFeedbackTheme feedbackTheme,
        IInteractionResponseService interactionResponseService,
        ContextInjectionService contextInjection
    )
    {
        _channelAPI = channelAPI;
        _userAPI = userAPI;
        _contextInjection = contextInjection;
        _interactionResponseService = interactionResponseService;

        this.Theme = feedbackTheme;
    }

    /// <inheritdoc cref="IInteractionResponseService.CreateDeferredMessageResponse(CancellationToken)" />
    public Task<Result> CreateDeferredMessageResponse(CancellationToken ct = default)
        => _interactionResponseService.CreateDeferredMessageResponse(ct);

    /// <summary>
    /// Send an informational message.
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendInfoAsync
    (
        Snowflake channel,
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendMessageAsync(channel, new FeedbackMessage(contents, this.Theme.Primary), target, options, ct);

    /// <summary>
    /// Send an informational message wherever is most appropriate to the current context.
    /// </summary>
    /// <remarks>
    /// This method will either create a followup message (if the context is an interaction) or a normal channel
    /// message.
    /// </remarks>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualInfoAsync
    (
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualMessageAsync(new FeedbackMessage(contents, this.Theme.Primary), target, options, ct);

    /// <summary>
    /// Send an informational message to the given user as a direct message.
    /// </summary>
    /// <param name="user">The user to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendPrivateInfoAsync
    (
        Snowflake user,
        string contents,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendPrivateMessageAsync(user, new FeedbackMessage(contents, this.Theme.Primary), options, ct);

    /// <summary>
    /// Send a positive, successful message.
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendSuccessAsync
    (
        Snowflake channel,
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendMessageAsync(channel, new FeedbackMessage(contents, this.Theme.Success), target, options, ct);

    /// <summary>
    /// Send a positive, successful message wherever is most appropriate to the current context.
    /// </summary>
    /// <remarks>
    /// This method will either create a followup message (if the context is an interaction) or a normal channel
    /// message.
    /// </remarks>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualSuccessAsync
    (
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualMessageAsync(new FeedbackMessage(contents, this.Theme.Success), target, options, ct);

    /// <summary>
    /// Send a positive, successful message to the given user as a direct message.
    /// </summary>
    /// <param name="user">The user to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendPrivateSuccessAsync
    (
        Snowflake user,
        string contents,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendPrivateMessageAsync(user, new FeedbackMessage(contents, this.Theme.Success), options, ct);

    /// <summary>
    /// Send a neutral message.
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendNeutralAsync
    (
        Snowflake channel,
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendMessageAsync(channel, new FeedbackMessage(contents, this.Theme.Secondary), target, options, ct);

    /// <summary>
    /// Send a neutral message wherever is most appropriate to the current context.
    /// </summary>
    /// <remarks>
    /// This method will either create a followup message (if the context is an interaction) or a normal channel
    /// message.
    /// </remarks>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualNeutralAsync
    (
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualMessageAsync(new FeedbackMessage(contents, this.Theme.Secondary), target, options, ct);

    /// <summary>
    /// Send a neutral message to the given user as a direct message.
    /// </summary>
    /// <param name="user">The user to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendPrivateNeutralAsync
    (
        Snowflake user,
        string contents,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendPrivateMessageAsync(user, new FeedbackMessage(contents, this.Theme.Secondary), options, ct);

    /// <summary>
    /// Send a warning message.
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendWarningAsync
    (
        Snowflake channel,
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendMessageAsync(channel, new FeedbackMessage(contents, this.Theme.Warning), target, options, ct);

    /// <summary>
    /// Send a warning message wherever is most appropriate to the current context.
    /// </summary>
    /// <remarks>
    /// This method will either create a followup message (if the context is an interaction) or a normal channel
    /// message.
    /// </remarks>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualWarningAsync
    (
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualMessageAsync(new FeedbackMessage(contents, this.Theme.Warning), target, options, ct);

    /// <summary>
    /// Send a warning message to the given user as a direct message.
    /// </summary>
    /// <param name="user">The user to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendPrivateWarningAsync
    (
        Snowflake user,
        string contents,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendPrivateMessageAsync(user, new FeedbackMessage(contents, this.Theme.Warning), options, ct);

    /// <summary>
    /// Send a negative error message.
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendErrorAsync
    (
        Snowflake channel,
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendMessageAsync(channel, new FeedbackMessage(contents, this.Theme.FaultOrDanger), target, options, ct);

    /// <summary>
    /// Send a negative error message wherever is most appropriate to the current context.
    /// </summary>
    /// <remarks>
    /// This method will either create a followup message (if the context is an interaction) or a normal channel
    /// message.
    /// </remarks>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualErrorAsync
    (
        string contents,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualMessageAsync(new FeedbackMessage(contents, this.Theme.FaultOrDanger), target, options, ct);

    /// <summary>
    /// Send a negative error message to the given user as a direct message.
    /// </summary>
    /// <param name="user">The user to send the message to.</param>
    /// <param name="contents">The contents of the message.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendPrivateErrorAsync
    (
        Snowflake user,
        string contents,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendPrivateMessageAsync(user, new FeedbackMessage(contents, this.Theme.FaultOrDanger), options, ct);

    /// <summary>
    /// Send a message.
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendMessageAsync
    (
        Snowflake channel,
        FeedbackMessage message,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContentAsync(channel, message.Message, message.Colour, target, options, ct);

    /// <summary>
    /// Send a contextual message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualMessageAsync
    (
        FeedbackMessage message,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualContentAsync(message.Message, message.Colour, target, options, ct);

    /// <summary>
    /// Send a private message.
    /// </summary>
    /// <param name="user">The user to send the message to.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendPrivateMessageAsync
    (
        Snowflake user,
        FeedbackMessage message,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendPrivateContentAsync(user, message.Message, message.Colour, options, ct);

    /// <summary>
    /// Sends the given embed to the given channel.
    /// </summary>
    /// <param name="channel">The channel to send the embed to.</param>
    /// <param name="embed">The embed.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendEmbedAsync
    (
        Snowflake channel,
        IEmbed embed,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendEmbedsAsync(channel, new[] { embed }, options, ct);

    /// <summary>
    /// Sends the given embed to the given channel.
    /// </summary>
    /// <param name="channel">The channel to send the embed to.</param>
    /// <param name="embeds">The embed.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> SendEmbedsAsync
    (
        Snowflake channel,
        IReadOnlyList<IEmbed> embeds,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        Result<IMessage> res = await _channelAPI.CreateMessageAsync
        (
            channel,
            isTTS: options?.IsTTS ?? default,
            embeds: new Optional<IReadOnlyList<IEmbed>>(embeds),
            allowedMentions: options?.AllowedMentions ?? default,
            components: options?.MessageComponents ?? default,
            attachments: options?.Attachments ?? default,
            ct: ct
        );

        return !res.IsSuccess
            ? Result.FromError(res)
            : Result.FromSuccess();
    }

    /// <summary>
    /// Sends the given embed to current context.
    /// </summary>
    /// <param name="embed">The embed.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<Result> SendContextualEmbedAsync
    (
        IEmbed embed,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendContextualEmbedsAsync(new[] { embed }, options, ct);

    /// <summary>
    /// Sends the given embed to current context.
    /// </summary>
    /// <param name="embeds">The embed.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> SendContextualEmbedsAsync
    (
        IReadOnlyList<IEmbed> embeds,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        if (_contextInjection.Context is null)
        {
            return new InvalidOperationError("Contextual sends require a context to be available.");
        }

        switch (_contextInjection.Context)
        {
            case MessageContext messageContext:
            {
                return await SendEmbedsAsync(messageContext.Message.ChannelID.Value, embeds, options, ct);
            }
            case InteractionContext:
            {
                Optional<MessageFlags> messageFlags = options?.MessageFlags ?? default;

                Result result = await _interactionResponseService.CreateContextualMessageResponse
                (
                    isTTS: options?.IsTTS ?? default,
                    embeds: new Optional<IReadOnlyList<IEmbed>>(embeds),
                    allowedMentions: options?.AllowedMentions ?? default,
                    components: options?.MessageComponents ?? default,
                    flags: messageFlags,
                    attachments: options?.Attachments ?? default,
                    ct: ct
                );

                if (!result.IsSuccess)
                    return result;

                HasEditedOriginalMessage = true;

                return result;
            }
            default:
            {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Sends the given embed to the given user in their private DM channel.
    /// </summary>
    /// <param name="user">The ID of the user to send the embed to.</param>
    /// <param name="embed">The embed.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> SendPrivateEmbedAsync
    (
        Snowflake user,
        Embed embed,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        Result<IChannel> getUserDM = await _userAPI.CreateDMAsync(user, ct);
        if (!getUserDM.IsDefined(out IChannel? dm))
            return Result.FromError(getUserDM);

        return await SendEmbedAsync(dm.ID, embed, options, ct);
    }

    /// <summary>
    /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
    /// </summary>
    /// <param name="channel">The channel to send the embed to.</param>
    /// <param name="contents">The contents to send.</param>
    /// <param name="color">The embed colour.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> SendContentAsync
    (
        Snowflake channel,
        string contents,
        Color color,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        foreach (Embed chunk in CreateContentChunks(target, color, contents))
        {
            Result send = await SendEmbedAsync(channel, chunk, options, ct);
            if (!send.IsSuccess)
                return send;
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
    /// </summary>
    /// <param name="contents">The contents to send.</param>
    /// <param name="color">The embed colour.</param>
    /// <param name="target">The target user to mention, if any.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> SendContextualContentAsync
    (
        string contents,
        Color color,
        Snowflake? target = null,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        foreach (Embed chunk in CreateContentChunks(target, color, contents))
        {
            Result send = await SendContextualEmbedAsync(chunk, options, ct);
            if (!send.IsSuccess)
                return send;
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sends the given string as one or more sequential embeds to the given user over DM, chunked into sets of 1024
    /// characters.
    /// </summary>
    /// <param name="user">The ID of the user to send the content to.</param>
    /// <param name="contents">The contents to send.</param>
    /// <param name="color">The embed colour.</param>
    /// <param name="options">The message options to use.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> SendPrivateContentAsync
    (
        Snowflake user,
        string contents,
        Color color,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        Result<IChannel> getUserDM = await _userAPI.CreateDMAsync(user, ct);
        if (!getUserDM.IsDefined(out IChannel? dm))
            return Result.FromError(getUserDM);

        return await SendContentAsync(dm.ID, contents, color, null, options, ct);
    }

    /// <summary>
    /// Creates a feedback embed.
    /// </summary>
    /// <param name="target">The invoking mentionable.</param>
    /// <param name="color">The colour of the embed.</param>
    /// <param name="contents">The contents of the embed.</param>
    /// <returns>A feedback embed.</returns>
    private static Embed CreateFeedbackEmbed(Snowflake? target, Color color, string contents)
    {
        if (target is null)
            return new Embed { Colour = color } with { Description = contents };

        return new Embed { Colour = color } with { Description = $"<@{target}> | {contents}" };
    }

    /// <summary>
    /// Chunks an input string into one or more embeds. Discord places an internal limit on embed lengths of 2048
    /// characters, and we collapse that into 1024 for readability's sake.
    /// </summary>
    /// <param name="target">The target user, if any.</param>
    /// <param name="color">The color of the embed.</param>
    /// <param name="contents">The complete contents of the message.</param>
    /// <returns>The chunked embeds.</returns>
    private static IEnumerable<Embed> CreateContentChunks(Snowflake? target, Color color, string contents)
    {
        // Sometimes the content is > 2048 in length. We'll chunk it into embeds of 1024 here.
        if (contents.Length < 1024)
        {
            yield return CreateFeedbackEmbed(target, color, contents.Trim());
            yield break;
        }

        string[] words = contents.Split(' ');
        StringBuilder messageBuilder = new();

        foreach (string word in words)
        {
            if (messageBuilder.Length >= 1024)
            {
                yield return CreateFeedbackEmbed(target, color, messageBuilder.ToString().Trim());
                messageBuilder.Clear();
            }

            messageBuilder.Append(word);
            messageBuilder.Append(' ');
        }

        if (messageBuilder.Length > 0)
            yield return CreateFeedbackEmbed(target, color, messageBuilder.ToString().Trim());
    }
}
