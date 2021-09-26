using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("team")]
    [Description("Commands that help with team generation")]
    [RequireContext(ChannelContext.Guild)]
    public class TeamGenerationCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IReplyService _replyService;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public TeamGenerationCommands(
            ICommandContext context,
            DiscordContext dbContext,
            IReplyService replyService,
            IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _dbContext = dbContext;
            _replyService = replyService;
            _guildAPI = guildAPI;
        }

        [Command("random-from-role")]
        [Description("Randomly sorts members with a certain role into teams")]
        public async Task<IResult> RandomFromRoleCommandAsync(
            [Description("Teams will be generated using members with this role")] IRole role,
            [Description("The number of teams to generate")] int numberOfTeams)
        {
            if (numberOfTeams < 2)
                return await _replyService.RespondWithUserErrorAsync("At least two teams are required", CancellationToken).ConfigureAwait(false);

            List<ulong> roleMembers = new();

            await foreach (Result<IReadOnlyList<IGuildMember>> users in _guildAPI.GetAllMembersAsync(_context.GuildID.Value, (m) => m.Roles.Contains(role.ID), CancellationToken))
            {
                if (!users.IsSuccess || users.Entity is null)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return Result.FromError(users);
                }

                roleMembers.AddRange(users.Entity.Select(m => m.User.Value.ID.Value));
            }

            return await SendRandomTeams(roleMembers, numberOfTeams, $"Build from the role { Formatter.RoleMention(role.ID)  }").ConfigureAwait(false);
        }

        [Command("random-from-group")]
        [Description("Randomly sorts members of a group into teams")]
        public async Task<IResult> RandomFromGroupCommandAsync(
            [Description("Teams will be generated using members of this group")] string groupName,
            [Description("The number of teams to generate")] int numberOfTeams)
        {
            if (numberOfTeams < 2)
                return await _replyService.RespondWithUserErrorAsync("At least two teams are required", CancellationToken).ConfigureAwait(false);

            Result<MemberGroup> group = await GetGroupAsync(groupName).ConfigureAwait(false);
            if (!group.IsSuccess)
                return Result.FromSuccess();

            return await SendRandomTeams(group.Entity.UserIds, numberOfTeams, $"Built from the group {Formatter.InlineQuote(group.Entity.GroupName)}").ConfigureAwait(false);
        }

        private async Task<IResult> SendRandomTeams(IList<ulong> memberPool, int teamCount, string embedDescription)
        {
            if (memberPool.Count < teamCount)
                return await _replyService.RespondWithUserErrorAsync("There cannot be more teams than team members", CancellationToken).ConfigureAwait(false);

            List<List<ulong>> teams = await CreateRandomTeams(memberPool, teamCount).ConfigureAwait(false);

            return await _replyService.RespondWithEmbedAsync(
                BuildTeamsEmbed(teams, "Random Teams", embedDescription),
                CancellationToken,
                allowedMentions: new AllowedMentions()).ConfigureAwait(false);
        }

        private async Task<List<List<ulong>>> CreateRandomTeams(IList<ulong> memberPool, int teamCount)
        {
            List<List<ulong>> teams = new(teamCount);

            await Task.Run(() =>
            {
                memberPool.Shuffle();

                for (int i = 0; i < teamCount; i++)
                    teams.Add(new List<ulong>());

                int teamPos = 0;
                for (int i = 0; i < memberPool.Count; i++)
                {
                    teams[teamPos++].Add(memberPool[i]);
                    if (teamPos == teamCount)
                        teamPos = 0;
                }
            }, CancellationToken).ConfigureAwait(false);

            return teams;
        }

        private static Embed BuildTeamsEmbed(List<List<ulong>> teams, string embedTitle, string embedDescription)
        {
            // TODO: Add support for teams that will exceed longer than 6000 characters. See #22
            List<EmbedField> embedFields = new();

            for (int i = 0; i < teams.Count; i++)
            {
                StringBuilder sb = new();

                foreach (ulong userID in teams[i])
                    sb.Append("- ").AppendLine(Formatter.UserMention(userID));

                embedFields.Add(new EmbedField($"Team {i + 1}", sb.ToString()));
            }

            return new Embed
            {
                Colour = BotConstants.DEFAULT_EMBED_COLOUR,
                Title = embedTitle,
                Description = embedDescription,
                Fields = embedFields
            };
        }

        private async Task<Result<MemberGroup>> GetGroupAsync(string groupName)
        {
            MemberGroup? group = await _dbContext.MemberGroups.FirstAsync(g => g.GuildId == _context.GuildID.Value.Value && g.GroupName == groupName, CancellationToken).ConfigureAwait(false);

            if (group is null)
            {
                await _replyService.RespondWithUserErrorAsync("A group with that name does not exist.", CancellationToken).ConfigureAwait(false);
                return new NotFoundError();
            }

            return group;
        }
    }
}
