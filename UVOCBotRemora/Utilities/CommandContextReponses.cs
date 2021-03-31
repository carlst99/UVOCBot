using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBotRemora.Config;

namespace UVOCBotRemora.Commands
{
    public class CommandContextReponses
    {
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestWebhookAPI _webhookAPI;
        private readonly IOptions<GeneralOptions> _options;

        public CommandContextReponses(IDiscordRestChannelAPI channelAPI, IDiscordRestWebhookAPI webhookAPI, IOptions<GeneralOptions> options)
        {
            _channelAPI = channelAPI;
            _webhookAPI = webhookAPI;
            _options = options;
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

        public async Task<Result<IMessage>> RespondToInteractionAsync(InteractionContext context, Optional<string?> content = default, Optional<IReadOnlyList<IEmbed>?> embeds = default, Optional<IAllowedMentions?> allowedMentions = default, CancellationToken ct = default)
        {
            return await _webhookAPI.EditOriginalInteractionResponseAsync(
                    new Snowflake(_options.Value.DiscordApplicationClientId),
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
