using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands;
using UVOCBot.Core.Model;
using UVOCBot.Model.Census;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Responders
{
    public class GuildMemberAddResponder : IResponder<IGuildMemberAdd>
    {
        private readonly ILogger<GuildMemberAddResponder> _logger;
        private readonly ICensusApiService _censusApi;
        private readonly IDbApiService _dbApi;
        private readonly IDiscordRestGuildAPI _guildApi;

        public GuildMemberAddResponder(
            ILogger<GuildMemberAddResponder> logger,
            ICensusApiService censusApi,
            IDbApiService dbApi,
            IDiscordRestGuildAPI guildApi)
        {
            _logger = logger;
            _censusApi = censusApi;
            _dbApi = dbApi;
            _guildApi = guildApi;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            if (gatewayEvent.User.Value is null)
                return Result.FromSuccess();

            Result<GuildWelcomeMessageDto> welcomeMessageResult = await _dbApi.GetGuildWelcomeMessageAsync(gatewayEvent.GuildID.Value, ct).ConfigureAwait(false);
            if (!welcomeMessageResult.IsSuccess)
            {
                _logger.LogError("Failed to retrieve GuildWelcomeMessage object: {error}", welcomeMessageResult.Error);
                return Result.FromError(welcomeMessageResult);
            }

            GuildWelcomeMessageDto welcomeMessage = welcomeMessageResult.Entity;
            if (!welcomeMessage.IsEnabled)
                return Result.FromSuccess();

            Result<IReadOnlyList<IRole>> guildRoles = await _guildApi.GetGuildRolesAsync(gatewayEvent.GuildID, ct).ConfigureAwait(false);
            if (!guildRoles.IsSuccess)
            {
                _logger.LogError("Could not get guild role list: {error}" + guildRoles.Error);
                return Result.FromError(guildRoles);
            }

            string messageContent = SubstituteMessageVariables(gatewayEvent, welcomeMessage.Message);

            // Assign default roles
            foreach (ulong roleId in welcomeMessage.DefaultRoles)
            {
                IRole? role = guildRoles.Entity.FirstOrDefault(r => r.ID.Value == roleId);
                if (role is not null)
                    await _guildApi.AddGuildMemberRoleAsync(gatewayEvent.GuildID, gatewayEvent.User.Value.ID, role.ID, ct).ConfigureAwait(false); // Not too worried about a failure here, as it's easily fixed
            }

            if (welcomeMessage.DoIngameNameGuess)
            {
                try
                {
                    List<NewOutfitMember> newMembers = await _censusApi.GetNewOutfitMembersAsync(welcomeMessage.OutfitId, 10, ct).ConfigureAwait(false);

                    // TODO: Fuzzy match, or most recent two if a good match couldn't be found.
                    // TODO: Abstract to new method
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to make a nickname guess.");
                }
            }
        }

        private static string SubstituteMessageVariables(IGuildMemberAdd gatewayEvent, string welcomeMessage)
        {
            if (gatewayEvent.User.Value is null)
                return welcomeMessage;

            return welcomeMessage.Replace("<name>", Formatter.UserMention(gatewayEvent.User.Value.ID));
        }
    }
}
