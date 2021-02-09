using DaybreakGames.Census;
using DaybreakGames.Census.Operators;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;
using UVOCBot.Model.Planetside;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    [Description("Commands that provide information about PlanetSide 2")]
    public class PlanetsideModule : BaseCommandModule
    {
        public const int MAX_FACTION_POPULATION = 400; // This is a rough value

        public IFisuApiService FisuApi { get; set; }
        public ICensusQueryFactory CensusFactory { get; set; }

        [Command("population")]
        [Aliases("pop", "server-status")]
        [Description("Gets the status and population of a server")]
        public async Task GetWorldStatusCommand(CommandContext ctx, [Description("The server to get the status of")] string server)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            if (!Enum.TryParse(server, true, out WorldType world))
            {
                await ctx.RespondAsync("That server does not exist").ConfigureAwait(false);
                return;
            }

            FisuPopulation population;
            try
            {
                population = await FisuApi.GetContinentPopulation((int)world).ConfigureAwait(false);
            }
            catch
            {
                population = new FisuPopulation
                {
                    Result = new System.Collections.Generic.List<FisuPopulation.ApiResult>
                    {
                        new FisuPopulation.ApiResult()
                    }
                };
            }

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
            {
                Color = Program.DEFAULT_EMBED_COLOUR,
                Description = await GetWorldStatusString(world).ConfigureAwait(false),
                Timestamp = DateTimeOffset.UtcNow,
                Title = world.ToString()
            };

            builder.AddField($":purple_circle: VS - {population.VS}", GetPopulationBar(population.VS));
            builder.AddField($":blue_circle: NC - {population.NC}", GetPopulationBar(population.NC));
            builder.AddField($":red_circle: TR - {population.TR}", GetPopulationBar(population.TR));

            await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        private async Task<string> GetWorldStatusString(WorldType world)
        {
            var query = CensusFactory.Create("world")
                .SetLanguage(CensusLanguage.English);
            query.Where("world_id").Equals((int)world);

            World worldData;
            try
            {
                worldData = await query.GetAsync<World>().ConfigureAwait(false);
            }
            catch
            {
                return "Status: Unknown :black_circle:";
            }

            if (worldData.State == "online")
                return "Status: Online :green_circle:";
            else if (worldData.State == "offline")
                return "Status: Offline :red_circle:";
            else if (worldData.State == "locked")
                return "Status: Locked :red_circle:";
            else
                return "Status: Unknown :black_circle:";
        }

        private string GetPopulationBar(int population)
        {
            double percentage = Math.Ceiling((double)population / MAX_FACTION_POPULATION * 10);
            double remainder = 10 - percentage;
            string result = string.Empty;

            for (int i = 0; i < percentage; i++)
                result += ":green_square:";

            for (int i = 0; i < remainder; i++)
                result += ":black_large_square:";

            return result;
        }
    }
}
