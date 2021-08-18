using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class ReplyService : IReplyService
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestInteractionAPI _interactionApi;

        public ReplyService(ICommandContext context, IDiscordRestChannelAPI channelApi, IDiscordRestInteractionAPI interactionApi)
        {
            _context = context;
            _channelApi = channelApi;
            _interactionApi = interactionApi;
        }

        /// <inheritdoc />
        public async Task<Result> TriggerTypingAsync(CancellationToken ct)
            => await _channelApi.TriggerTypingIndicatorAsync(_context.ChannelID, ct).ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondWithEmbedAsync(IEmbed embed, CancellationToken ct, Optional<FileData> file = default, Optional<IAllowedMentions> allowedMentions = default)
        {
            if (_context is InteractionContext)
                return await RespondToInteractionAsync(ct, default, file, new List<IEmbed> { embed }, allowedMentions).ConfigureAwait(false);
            else
                return await RespondToMessageAsync(ct, default, embed: new List<IEmbed>() { embed }, allowedMentions: allowedMentions).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondWithEmbedAsync(IReadOnlyList<IEmbed> embed, CancellationToken ct, Optional<FileData> file = default, Optional<IAllowedMentions> allowedMentions = default)
        {
            if (_context is InteractionContext)
                return await RespondToInteractionAsync(ct, default, file, new Optional<IReadOnlyList<IEmbed>>(embed), allowedMentions).ConfigureAwait(false);
            else
                return await RespondToMessageAsync(ct, default, embed: new Optional<IReadOnlyList<IEmbed>>(embed), allowedMentions: allowedMentions).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondWithContentAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
        {
            if (_context is InteractionContext)
                return await RespondToInteractionAsync(ct, content, allowedMentions: allowedMentions).ConfigureAwait(false);
            else
                return await RespondToMessageAsync(ct, content, default, allowedMentions: allowedMentions).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondWithSuccessAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
            => await RespondWithEmbedAsync(GetSuccessEmbed(content), ct, default, allowedMentions).ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondWithUserErrorAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
            => await RespondWithEmbedAsync(GetUserErrorEmbed(content), ct, default, allowedMentions).ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondWithErrorAsync(CancellationToken ct, string content = "Something went wrong! Please try again.", Optional<IAllowedMentions> allowedMentions = default)
            => await RespondWithEmbedAsync(GetErrorEmbed(content), ct, default, allowedMentions).ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondToInteractionAsync(CancellationToken ct, Optional<string> content = default, Optional<FileData> file = default, Optional<IReadOnlyList<IEmbed>> embeds = default, Optional<IAllowedMentions> allowedMentions = default)
        {
            if (_context is not InteractionContext ictx)
                return new InvalidOperationError("Cannot respond to a non-interaction context with an interaction response.");

            return await _interactionApi.CreateFollowupMessageAsync(
                ictx.ApplicationID,
                ictx.Token,
                content,
                file: file,
                embeds: embeds,
                allowedMentions: allowedMentions,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<IMessage>> RespondToMessageAsync(CancellationToken ct, Optional<string> content = default, Optional<string> nonce = default, Optional<bool> isTTS = default, Optional<FileData> file = default, Optional<IReadOnlyList<IEmbed>> embed = default, Optional<IAllowedMentions> allowedMentions = default, Optional<IMessageReference> messageReference = default, Optional<IReadOnlyList<IMessageComponent>> components = default)
        {
            return await _channelApi.CreateMessageAsync(
                _context.ChannelID,
                content,
                nonce,
                isTTS,
                file,
                embed,
                allowedMentions,
                messageReference,
                components,
                stickerIds: default,
                ct).ConfigureAwait(false);
        }

        public IEmbed GetSuccessEmbed(string content)
            => new Embed
            {
                Colour = BotConstants.DEFAULT_EMBED_COLOUR,
                Description = content
            };

        public IEmbed GetErrorEmbed(string content)
            => new Embed
            {
                Colour = Color.Red,
                Description = content,
                Footer = new EmbedFooter("Recurring problem? Report it at https://github.com/carlst99/UVOCBot")
            };

        public IEmbed GetUserErrorEmbed(string content)
            => new Embed
            {
                Colour = Color.Red,
                Description = content,
            };
    }
}
