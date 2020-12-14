using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Extensions;
using System;
using UVOCBot.Exceptions;
using System.Text;

namespace UVOCBot.Commands
{
    [Group("team")]
    [Aliases("teams")]
    [Description("Commands that help with generating team lists")]
    [RequireGuild]
    public class TeamsModule : BaseCommandModule
    {
        [Command("random-teams")]
        [Aliases("rt", "random")]
        [Description("Generates any number of random teams from a ")]
        public async Task RandomTeamsCommand(
            CommandContext ctx,
            [Description("Anyone with this role will be randomised into teams")] DiscordRole randomiseMembersOf,
            [Description("These people will be the team captains")] params DiscordMember[] captains)
        {
            IEnumerable<DiscordMember> allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);
            List<DiscordMember> roleMembers = allMembers.Where(m => m.Roles.Contains(randomiseMembersOf)).ToList();

            foreach (DiscordMember c in captains)
            {
                if (roleMembers.Contains(c))
                    roleMembers.Remove(c);
            }

            if (roleMembers.Count < captains.Length)
            {
                await ctx.RespondAsync("There cannot be more team captains than team members").ConfigureAwait(false);
                return;
            }

            List<DiscordMember> teamCaptains;
            try
            {
                teamCaptains = await CheckForDuplicateMembers(captains).ConfigureAwait(false);
            } catch (DuplicateItemException<DiscordMember> diex)
            {
                await ctx.RespondAsync("There cannot be duplicate captains - " + diex.DuplicateItem.DisplayName).ConfigureAwait(false);
                return;
            }

            List<List<DiscordMember>> teams = await CreateRandomTeams(roleMembers, teamCaptains.Count).ConfigureAwait(false);

            await ctx.RespondAsync(embed: BuildTeamsEmbed(teams, teamCaptains, "Random Teams")).ConfigureAwait(false);
        }

        [Command("random-teams")]
        public async Task RandomTeamsCommand(
            CommandContext ctx,
            [Description("Anyone with this role will be randomised into teams")] DiscordRole randomiseMembersOf,
            [Description("The number of teams to generate")] int numberOfTeams)
        {
            IEnumerable<DiscordMember> allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);
            List<DiscordMember> roleMembers = allMembers.Where(m => m.Roles.Contains(randomiseMembersOf)).ToList();

            if (roleMembers.Count < numberOfTeams)
            {
                await ctx.RespondAsync("There cannot be more teams than team members").ConfigureAwait(false);
                return;
            }
        }

        [Command("random-teams")]
        public async Task RandomTeamsCommand(
            CommandContext ctx,
            [Description("The number of teams to generate")] int numberOfTeams,
            [Description("True if the first members listed should be designated as team captains")] bool firstListedMembersAsCaptains = false,
            [Description("The members from whom to form teams")] params DiscordMember[] teamMembers)
        {
            if (numberOfTeams > teamMembers.Length)
            {
                await ctx.RespondAsync("There cannot be more teams than team members").ConfigureAwait(false);
                return;
            }
        }

        private async Task<List<List<DiscordMember>>> CreateRandomTeams(List<DiscordMember> memberPool, int teamCount = 2)
        {
            if (teamCount < 1)
                throw new ArgumentOutOfRangeException(nameof(teamCount), "At least one team is required");

            List<List<DiscordMember>> teams = new List<List<DiscordMember>>(teamCount);

            await Task.Run(() =>
            {
                for (int i = 0; i < teamCount; i++)
                    teams.Add(new List<DiscordMember>());

                int teamPos = 0;
                for (int i = 0; i < memberPool.Count; i++)
                {
                    teams[teamPos++].Add(memberPool[i]);
                    if (teamPos == memberPool.Count)
                        teamPos = 0;
                }
            }).ConfigureAwait(false);

            //int teamMemberCount = memberPool.Count / teamCount;
            //if (teamMemberCount == 0)
            //    throw new ArgumentOutOfRangeException(nameof(teamCount), "Each team must have at least one member");

            //await Task.Run(() =>
            //{
            //    memberPool.Shuffle();

            //    for (int i = 0; i < memberPool.Count; i++)
            //    {
            //        List<DiscordMember> teamMembers = new List<DiscordMember>();
            //        for (int j = i * teamMemberCount; j < (i * teamMemberCount) + teamMemberCount; j++)
            //            teamMembers.Add(memberPool[j]);

            //        teams.Add(teamMembers);
            //    }

            //    if (teamMemberCount * teamCount < memberPool.Count)
            //    {
            //        int pos = 0;
            //        for (int i = teamMemberCount * teamCount; i < memberPool.Count; i++)
            //        {
            //            teams[pos++].Add(memberPool[i]);
            //            if (pos == memberPool.Count)
            //                pos = 0;
            //        }
            //    }
            //}).ConfigureAwait(false);

            return teams;
        }

        /// <summary>
        /// Checks that there are no duplicated members in a list
        /// </summary>
        /// <param name="captains"></param>
        /// <returns></returns>
        /// <exception cref="DuplicateItemException{T}">Thrown if a duplicate member is detected. The <see cref="DuplicateItemException{T}.DuplicateItem"/> property is set to the member that was duplicated</exception>
        private async Task<List<DiscordMember>> CheckForDuplicateMembers(IEnumerable<DiscordMember> captains)
        {
            List<DiscordMember> teamCaptains = new List<DiscordMember>();

            await Task.Run(() =>
            {
                foreach (DiscordMember element in captains)
                {
                    if (teamCaptains.Contains(element))
                        throw new DuplicateItemException<DiscordMember>(element);
                    else
                        teamCaptains.Add(element);
                }
            }).ConfigureAwait(false);

            return teamCaptains;
        }

        private DiscordEmbed BuildTeamsEmbed(List<List<DiscordMember>> teams, List<DiscordMember> captains, string embedTitle)
        {
            // TODO: Add support for teams that will exceed longer than 6000 characters

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Aquamarine,
                Timestamp = DateTimeOffset.UtcNow,
                Title = embedTitle
            };

            for (int i = 0; i < teams.Count; i++)
            {
                StringBuilder sb = new StringBuilder();
                foreach (DiscordMember m in teams[i])
                    sb.Append("- ").AppendLine(m.DisplayName);

                builder.AddField($"Team {i} - Captain {captains[i].DisplayName}", sb.ToString());
            }

            return builder.Build();
        }
    }
}
