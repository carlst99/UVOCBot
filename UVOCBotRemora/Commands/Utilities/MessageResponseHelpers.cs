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

namespace UVOCBotRemora.Commands
{
    public class MessageResponseHelpers
    {
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestWebhookAPI _webhookAPI;

        public MessageResponseHelpers(IDiscordRestChannelAPI channelAPI, IDiscordRestWebhookAPI webhookAPI)
        {
            _channelAPI = channelAPI;
            _webhookAPI = webhookAPI;
        }

        public async Task<Result> TriggerTypingAsync(ICommandContext context, CancellationToken ct)
        {
            return await _channelAPI.TriggerTypingIndicatorAsync(context.ChannelID, ct).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondWithEmbedAsync(ICommandContext context, IEmbed embed, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
        {
            if (context is InteractionContext ictx)
                return await RespondToInteractionAsync(ictx, ct, default, new List<IEmbed> { embed }, allowedMentions).ConfigureAwait(false);
            else
                return await RespondToMessageAsync(context, ct, default, embed: new Optional<IEmbed>(embed), allowedMentions: allowedMentions).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondWithContentAsync(ICommandContext context, string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
        {
            if (context is InteractionContext ictx)
                return await RespondToInteractionAsync(ictx, ct, content, allowedMentions: allowedMentions).ConfigureAwait(false);
            else
                return await RespondToMessageAsync(context, ct, content, default, allowedMentions: allowedMentions).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondWithSuccessAsync(ICommandContext context, string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
        {
            Embed embed = new()
            {
                Colour = Program.DEFAULT_EMBED_COLOUR,
                Description = content
            };
            return await RespondWithEmbedAsync(context, embed, ct, allowedMentions).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondWithErrorAsync(ICommandContext context, string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default)
        {
            Embed embed = new()
            {
                Colour = Color.Red,
                Description = content
            };
            return await RespondWithEmbedAsync(context, embed, ct, allowedMentions).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondToInteractionAsync(InteractionContext context, CancellationToken ct, Optional<string> content = default, Optional<IReadOnlyList<IEmbed>> embeds = default, Optional<IAllowedMentions> allowedMentions = default)
        {
            return await _webhookAPI.CreateFollowupMessageAsync(
                context.ApplicationID,
                context.Token,
                content,
                embeds: embeds,
                allowedMentions: allowedMentions,
                ct: ct).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondToMessageAsync(ICommandContext context, CancellationToken ct, Optional<string> content = default, Optional<string> nonce = default, Optional<bool> isTTS = default, Optional<FileData> file = default, Optional<IEmbed> embed = default, Optional<IAllowedMentions> allowedMentions = default, Optional<IMessageReference> messageReference = default)
        {
            return await _channelAPI.CreateMessageAsync(context.ChannelID, content, nonce, isTTS, file, embed, allowedMentions, messageReference, ct).ConfigureAwait(false);
        }
    }
}
