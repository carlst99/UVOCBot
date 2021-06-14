using FuzzySharp;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
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

namespace UVOCBot.Services
{
    public class MemberAddMessageService : IMemberAddMessageService
    {
        private readonly ILogger<MemberAddMessageService> _logger;
        private readonly ICensusApiService _censusApi;
        private readonly IDbApiService _dbApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;

        private IReadOnlyList<IRole>? _guildRoles;

        public MemberAddMessageService(
            ILogger<MemberAddMessageService> logger,
            ICensusApiService censusApi,
            IDbApiService dbApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi)
        {
            _logger = logger;
            _censusApi = censusApi;
            _dbApi = dbApi;
            _channelApi = channelApi;
            _guildApi = guildApi;
        }

        public async Task<Result> SendWelcomeMessage(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
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

            // Add the alternate roles button
            List<ButtonComponent> messageButtons = new();
            if (welcomeMessage.AlternateRoles.Count > 0 && !string.IsNullOrEmpty(welcomeMessage.AlternateRoleLabel))
            {
                messageButtons.Add(new ButtonComponent(
                    ButtonComponentStyle.Danger,
                    welcomeMessage.AlternateRoleLabel,
                    CustomID: ComponentIdFormatter.GetId(ComponentAction.WelcomeMessageSetAlternate, gatewayEvent.User.Value.ID.Value.ToString())));
            }

            // Assign default roles
            await AssignRoles(gatewayEvent.GuildID, gatewayEvent.User.Value.ID, welcomeMessage.DefaultRoles, ct).ConfigureAwait(false);

            // Make some nickname guesses
            IEnumerable<string> nicknameGuesses = new List<string>();
            if (welcomeMessage.DoIngameNameGuess)
                nicknameGuesses = await DoFuzzyNicknameGuess(gatewayEvent.User.Value.Username, welcomeMessage.OutfitId, ct).ConfigureAwait(false);

            foreach (string nickname in nicknameGuesses)
            {
                messageButtons.Add(new ButtonComponent(
                    ButtonComponentStyle.Primary,
                    "My PlanetSide 2 character is: " + nickname,
                    CustomID: ComponentIdFormatter.GetId(ComponentAction.WelcomeMessageNicknameGuess, gatewayEvent.User.Value.ID.Value.ToString())));
            }

            string messageContent = SubstituteMessageVariables(gatewayEvent, welcomeMessage.Message);

            Result<IMessage> sendWelcomeMessageResult = await _channelApi.CreateMessageAsync(
                new Snowflake(welcomeMessage.ChannelId),
                messageContent,
                isTTS: false,
                allowedMentions: new AllowedMentions(new List<MentionType>() { MentionType.Users }),
                components: new List<IMessageComponent>()
                {
                    new ActionRowComponent(messageButtons)
                },
                ct: ct).ConfigureAwait(false);

            return sendWelcomeMessageResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendWelcomeMessageResult);
        }

        private async Task<IEnumerable<string>> DoFuzzyNicknameGuess(string username, ulong outfitId, CancellationToken ct = default)
        {
            const int minMatchRatio = 65;
            const int maxGuesses = 2;
            List<Tuple<string, int>> nicknameGuesses = new();

            try
            {
                List<NewOutfitMember> newMembers = await _censusApi.GetNewOutfitMembersAsync(outfitId, 10, ct).ConfigureAwait(false);
                newMembers.Sort((NewOutfitMember x, NewOutfitMember y) => y.MemberSince.CompareTo(x.MemberSince));

                for (int i = 0; i < newMembers.Count; i++)
                {
                    NewOutfitMember m = newMembers[i];

                    int matchRatio = Fuzz.PartialRatio(m.CharacterName.Name.First, username);
                    if (matchRatio > minMatchRatio)
                    {
                        nicknameGuesses.Add(new Tuple<string, int>(m.CharacterName.Name.First, matchRatio));
                        newMembers.RemoveAt(i);
                        i--;
                    }
                }

                nicknameGuesses.Sort((Tuple<string, int> x, Tuple<string, int> y) => y.Item2.CompareTo(x.Item2));

                if (nicknameGuesses.Count < maxGuesses)
                    nicknameGuesses.AddRange(newMembers.Take(maxGuesses - nicknameGuesses.Count).Select(m => new Tuple<string, int>(m.CharacterName.Name.First, 0)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to make a nickname guess.");
            }

            return nicknameGuesses.Take(2).Select(g => g.Item1);
        }

        private static string SubstituteMessageVariables(IGuildMemberAdd gatewayEvent, string welcomeMessage)
        {
            if (gatewayEvent.User.Value is null)
                return welcomeMessage;

            return welcomeMessage.Replace("<name>", Formatter.UserMention(gatewayEvent.User.Value.ID));
        }

        private async Task<Result> AssignRoles(Snowflake guildId, Snowflake userId, IEnumerable<ulong> roles, CancellationToken ct = default)
        {
            if (_guildRoles is null)
            {
                Result<IReadOnlyList<IRole>> guildRolesResult = await _guildApi.GetGuildRolesAsync(guildId, ct).ConfigureAwait(false);
                if (!guildRolesResult.IsSuccess)
                {
                    _logger.LogError("Could not get guild role list: {error}" + guildRolesResult.Error);
                    return Result.FromError(guildRolesResult);
                }

                _guildRoles = guildRolesResult.Entity;
            }

            foreach (ulong roleId in roles)
            {
                IRole? role = _guildRoles.FirstOrDefault(r => r.ID.Value == roleId);
                if (role is not null)
                    await _guildApi.AddGuildMemberRoleAsync(guildId, userId, role.ID, ct).ConfigureAwait(false); // Not too worried about a failure here, as it's easily fixed by a guild admin
            }

            return Result.FromSuccess();
        }
    }
}
