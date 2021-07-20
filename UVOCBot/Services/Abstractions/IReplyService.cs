using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IReplyService
    {
        Task<Result> TriggerTypingAsync(CancellationToken ct);
        Task<Result<IMessage>> RespondWithEmbedAsync(IEmbed embed, CancellationToken ct, Optional<FileData> file = default, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondWithEmbedAsync(IReadOnlyList<IEmbed> embed, CancellationToken ct, Optional<FileData> file = default, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondWithContentAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondWithSuccessAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondWithUserErrorAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondWithErrorAsync(string content, CancellationToken ct, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondToInteractionAsync(CancellationToken ct, Optional<string> content = default, Optional<FileData> file = default, Optional<IReadOnlyList<IEmbed>> embeds = default, Optional<IAllowedMentions> allowedMentions = default);
        Task<Result<IMessage>> RespondToMessageAsync(CancellationToken ct, Optional<string> content = default, Optional<string> nonce = default, Optional<bool> isTTS = default, Optional<FileData> file = default, Optional<IReadOnlyList<IEmbed>> embed = default, Optional<IAllowedMentions> allowedMentions = default, Optional<IMessageReference> messageReference = default, Optional<IReadOnlyList<IMessageComponent>> components = default);
    }
}
