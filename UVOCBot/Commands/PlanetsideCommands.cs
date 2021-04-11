using DaybreakGames.Census;
using DaybreakGames.Census.Operators;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBot.Model.Planetside;
using UVOCBot.Services;

namespace UVOCBot.Commands
{
    public class PlanetsideCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IAPIService _dbAPI;
        private readonly ICensusQueryFactory _censusQueryFactory;
        private readonly IFisuApiService _fisuAPI;

        public PlanetsideCommands(ICommandContext context, MessageResponseHelpers responder, IAPIService dbAPI, ICensusQueryFactory censusQueryFactory, IFisuApiService fisuAPI)
        {
            _context = context;
            _responder = responder;
            _dbAPI = dbAPI;
            _censusQueryFactory = censusQueryFactory;
            _fisuAPI = fisuAPI;
        }

        [Command("population")]
        [Description("Gets the population of a PlanetSide server")]
        public async Task<IResult> GetServerPopulationCommandAsync(
            [Description("Set your default server with `default-server`")] [DiscordTypeHint(TypeHint.String)] WorldType server = 0)
        {
            if (server == 0)
            {
                if (!_context.GuildID.HasValue)
                    return await _responder.RespondWithErrorAsync(_context, "To use this command in a DM you must provide a server.", ct: CancellationToken).ConfigureAwait(false);

                PlanetsideSettingsDTO settings = await _dbAPI.GetPlanetsideSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
                if (settings.DefaultWorld == null)
                {
                    return await _responder.RespondWithErrorAsync(
                        _context,
                        $"You haven't set a default server! Please do so using the {Formatter.InlineQuote("default-server")} command.",
                        ct: CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    WorldType world = (WorldType)(int)settings.DefaultWorld;
                    return await SendWorldPopulation(world).ConfigureAwait(false);
                }
            }
            else
            {
                return await SendWorldPopulation(server).ConfigureAwait(false);
            }
        }

        [Command("default-server")]
        [Description("Sets the default world for planetside-related commands")]
        [RequireContext(ChannelContext.Guild)]
        [RequireUserGuildPermission(DiscordPermission.ManageGuild)]
        public async Task<IResult> DefaultWorldCommand([DiscordTypeHint(TypeHint.String)] WorldType server)
        {
            PlanetsideSettingsDTO settings = await _dbAPI.GetPlanetsideSettingsAsync(_context.GuildID.Value.Value).ConfigureAwait(false);
            settings.DefaultWorld = (int)server;
            await _dbAPI.UpdatePlanetsideSettings(_context.GuildID.Value.Value, settings).ConfigureAwait(false);

            return await _responder.RespondWithSuccessAsync(
                _context,
                $"{Formatter.Emoji("earth_asia")} Your default server has been set to {Formatter.InlineQuote(server.ToString())}",
                ct: CancellationToken).ConfigureAwait(false);
        }

        private async Task<IResult> SendWorldPopulation(WorldType world)
        {
            FisuPopulation population;
            try
            {
                population = await _fisuAPI.GetContinentPopulation((int)world).ConfigureAwait(false);
            }
            catch
            {
                population = new FisuPopulation
                {
                    Result = new List<FisuPopulation.ApiResult>
                    {
                        new FisuPopulation.ApiResult()
                    }
                };
            }

            Embed embed = new()
            {
                Colour = Program.DEFAULT_EMBED_COLOUR,
                Description = await GetWorldStatusString(world).ConfigureAwait(false),
                Title = world.ToString() + " - " + population.Total.ToString(),
                Footer = new EmbedFooter("Data gratefully taken from ps2.fisu.pw"),
                Fields=  new List<EmbedField>
                {
                    new EmbedField($"{Formatter.Emoji("purple_circle")} VS - {population.VS}", GetEmbedPopulationBar(population.VS, population.Total)),
                    new EmbedField($"{Formatter.Emoji("blue_circle")} NC - {population.NC}", GetEmbedPopulationBar(population.NC, population.Total)),
                    new EmbedField($"{Formatter.Emoji("red_circle")} TR - {population.TR}", GetEmbedPopulationBar(population.TR, population.Total)),
                    new EmbedField($"{Formatter.Emoji("white_circle")} NS - {population.NS}", GetEmbedPopulationBar(population.NS, population.Total))
                }
            };

            return await _responder.RespondWithEmbedAsync(_context, embed, CancellationToken).ConfigureAwait(false);
        }

        private async Task<string> GetWorldStatusString(WorldType world)
        {
            var query = _censusQueryFactory.Create("world")
                .SetLanguage(CensusLanguage.English);
            query.Where("world_id").Equals((int)world);

            World worldData;
            try
            {
                worldData = await query.GetAsync<World>().ConfigureAwait(false);
            }
            catch
            {
                return $"Status: Unknown {Formatter.Emoji("black_circle")}";
            }

            if (worldData.State == "online")
                return $"Status: Online {Formatter.Emoji("green_circle")}";
            else if (worldData.State == "offline")
                return $"Status: Offline {Formatter.Emoji("red_circle")}";
            else if (worldData.State == "locked")
                return $"Status: Locked {Formatter.Emoji("red_circle")}";
            else
                return $"Status: Unknown {Formatter.Emoji("black_circle")}";
        }

        private static string GetEmbedPopulationBar(int population, int totalPopulation)
        {
            // Can't divide by zero!
            if (totalPopulation == 0 || population > totalPopulation)
                return Formatter.Emoji("black_large_square");

            double tensPercentage = Math.Ceiling((double)population / totalPopulation * 10);
            double remainder = 10 - tensPercentage;
            string result = string.Empty;

            for (int i = 0; i < tensPercentage; i++)
                result += Formatter.Emoji("green_square");

            for (int i = 0; i < remainder; i++)
                result += Formatter.Emoji("black_large_square");

            string stringPercentage = ((double)population / totalPopulation * 100).ToString("F1");
            result += $"{Formatter.Bold("   ")}({ stringPercentage }%)";

            return result;
        }
    }
}
