using Microsoft.Extensions.Options;
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
using UVOCBotRemora.Config;

namespace UVOCBotRemora.Commands
{
    public class CommandContextReponses
    {
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestWebhookAPI _webhookAPI;

        public CommandContextReponses(IDiscordRestChannelAPI channelAPI, IDiscordRestWebhookAPI webhookAPI)
        {
            _channelAPI = channelAPI;
            _webhookAPI = webhookAPI;
        }

        public async Task<Result> TriggerTypingAsync(ICommandContext context, CancellationToken ct)
        {
            return await _channelAPI.TriggerTypingIndicatorAsync(context.ChannelID, ct).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondAsync(ICommandContext context, Optional<string?> content = default, Optional<IEmbed> embed = default, Optional<IAllowedMentions?> allowedMentions = default, CancellationToken ct = default)
        {
            if (context is InteractionContext ictx)
            {
                if (embed.HasValue)
                    return await RespondToInteractionAsync(ictx, content, new List<IEmbed> { embed.Value }, allowedMentions, ct).ConfigureAwait(false);
                else
                    return await RespondToInteractionAsync(ictx, content, allowedMentions: allowedMentions, ct: ct).ConfigureAwait(false);
            }
            else
            {
                Optional<string> contentNotNull = new(content.Value);
                Optional<IAllowedMentions> allowedMentionsNotNull = new(allowedMentions.Value);

                return await RespondWithMessageAsync(context, contentNotNull, embed: embed, allowedMentions: allowedMentionsNotNull, ct: ct).ConfigureAwait(false);
            }
        }

        public async Task<Result<IMessage>> RespondWithSuccessAsync(ICommandContext context, string content, Optional<IAllowedMentions?> allowedMentions = default, CancellationToken ct = default)
        {
            Embed embed = new()
            {
                Colour = Color.Green,
                Description = content
            };
            return await RespondAsync(context, embed: embed, allowedMentions: allowedMentions, ct: ct).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondWithErrorAsync(ICommandContext context, string content, Optional<IAllowedMentions?> allowedMentions = default, CancellationToken ct = default)
        {
            Embed embed = new()
            {
                Colour = Color.Red,
                Description = content
            };
            return await RespondAsync(context, embed: embed, allowedMentions: allowedMentions, ct: ct).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondToInteractionAsync(InteractionContext context, Optional<string?> content = default, Optional<IReadOnlyList<IEmbed>?> embeds = default, Optional<IAllowedMentions?> allowedMentions = default, CancellationToken ct = default)
        {
            return await _webhookAPI.EditOriginalInteractionResponseAsync(
                    context.ApplicationID,
                    context.Token,
                    content,
                    embeds,
                    allowedMentions,
                    ct).ConfigureAwait(false);
        }

        public async Task<Result<IMessage>> RespondWithMessageAsync(ICommandContext context, Optional<string> content = default, Optional<string> nonce = default, Optional<bool> isTTS = default, Optional<FileData> file = default, Optional<IEmbed> embed = default, Optional<IAllowedMentions> allowedMentions = default, Optional<IMessageReference> messageReference = default, CancellationToken ct = default)
        {
            return await _channelAPI.CreateMessageAsync(context.ChannelID, content, nonce, isTTS, file, embed, allowedMentions, messageReference, ct).ConfigureAwait(false);
        }
    }
}
