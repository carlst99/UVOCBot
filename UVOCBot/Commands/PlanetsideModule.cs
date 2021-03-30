using DaybreakGames.Census;
using DaybreakGames.Census.Operators;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBot.Model.Planetside;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    [Description("Commands that provide information about PlanetSide 2")]
    public class PlanetsideModule : BaseCommandModule
    {
        public IFisuApiService FisuApi { get; set; }
        public ICensusQueryFactory CensusFactory { get; set; }
        public IApiService DbApi { get; set; }

        [Command("population")]
        [Description("Gets the status and population of your default server")]
        [RequireGuild]
        public async Task GetWorldStatusCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            PlanetsideSettingsDTO settings = await DbApi.GetPlanetsideSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (settings.DefaultWorld == null)
            {
                await ctx.RespondAsync("You haven't set a default server! Please do so using the `default-server` command").ConfigureAwait(false);
                return;
            }
            else
            {
                WorldType world = (WorldType)((int)settings.DefaultWorld);
                await GetWorldStatusCommand(ctx, world.ToString()).ConfigureAwait(false);
            }
        }

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

            DiscordEmbedBuilder builder = new()
            {
                Color = Program.DEFAULT_EMBED_COLOUR,
                Description = await GetWorldStatusString(world).ConfigureAwait(false),
                Timestamp = DateTimeOffset.UtcNow,
                Title = world.ToString() + " - " + population.Total.ToString(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Data gratefully taken from ps2.fisu.pw"
                }
            };

            builder.AddField($":purple_circle: VS - {population.VS}", GetPopulationBar(population.VS, population.Total));
            builder.AddField($":blue_circle: NC - {population.NC}", GetPopulationBar(population.NC, population.Total));
            builder.AddField($":red_circle: TR - {population.TR}", GetPopulationBar(population.TR, population.Total));
            builder.AddField($":white_circle: NS - {population.NS}", GetPopulationBar(population.NS, population.Total));

            await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("default-server")]
        [Description("Sets the default world for planetside-related commands")]
        [RequireGuild]
        public async Task DefaultWorldCommand(CommandContext ctx, string server)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            if (!Enum.TryParse(server, true, out WorldType world))
            {
                await ctx.RespondAsync("That server does not exist").ConfigureAwait(false);
                return;
            }

            PlanetsideSettingsDTO settings = await DbApi.GetPlanetsideSettingsAsync(ctx.Guild.Id).ConfigureAwait(false);
            settings.DefaultWorld = (int)world;
            await DbApi.UpdatePlanetsideSettings(ctx.Guild.Id, settings).ConfigureAwait(false);

            await ctx.RespondAsync($"Your default server has been set to `{world}`").ConfigureAwait(false);
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

        private string GetPopulationBar(int population, int totalPopulation)
        {
            double tensPercentage = Math.Ceiling((double)population / totalPopulation * 10);
            double remainder = 10 - tensPercentage;
            string result = string.Empty;

            for (int i = 0; i < tensPercentage; i++)
                result += ":green_square:";

            for (int i = 0; i < remainder; i++)
                result += ":black_large_square:";

            string stringPercentage = ((double)population / totalPopulation * 100).ToString("F1");
            result += $"**   **({stringPercentage}%)";

            return result;
        }
    }
}
