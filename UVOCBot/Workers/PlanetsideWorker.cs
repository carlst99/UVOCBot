using DaybreakGames.Census;
using DaybreakGames.Census.Operators;
using DaybreakGames.Census.Stream;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.Planetside;
using UVOCBot.Services;
using Websocket.Client;

namespace UVOCBot.Workers
{
    public sealed class PlanetsideWorker : BackgroundService
    {
        private readonly ICensusStreamClient _censusClient;
        private readonly ICensusQueryFactory _censusQueryFactory;
        private readonly IApiService _dbApi;

        private readonly CensusStreamSubscription _censusSubscription = new CensusStreamSubscription
        {
            Worlds = new[] { "all" },
            EventNames = new[] { "ContinentLock", "ContinentUnlock", "MetagameEvent" }
        };

        public PlanetsideWorker(ICensusStreamClient censusClient, ICensusQueryFactory censusQueryFactory, IApiService dbApi)
        {
            _censusClient = censusClient;
            _censusQueryFactory = censusQueryFactory;
            _dbApi = dbApi;

            _censusClient.OnConnect(OnCensusConnect);
            _censusClient.OnMessage(OnCensusMessage);
            _censusClient.OnDisconnect(OnCensusDisconnect);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _censusClient.ConnectAsync().ConfigureAwait(false);

            while (true)
            {
                await GetOutfitOnlineStatus().ConfigureAwait(false);

                await Task.Delay(300000, stoppingToken).ConfigureAwait(false); // Work every 5min
            }
        }

        private async Task GetOutfitOnlineStatus()
        {
            CensusQuery query = _censusQueryFactory.Create("outfit");
            query.Where("outfit_id").Equals(37570391403474619);
            query.ShowFields("name", "outfit_id");

            CensusJoin outfitMemberJoin = query.JoinService("outfit_member");
            outfitMemberJoin.WithInjectAt("members");
            outfitMemberJoin.ShowFields("character_id");
            outfitMemberJoin.IsOuterJoin(false);
            outfitMemberJoin.IsList(true);

            CensusJoin characterJoin = outfitMemberJoin.JoinService("character");
            characterJoin.OnField("character_id");
            characterJoin.WithInjectAt("character");
            characterJoin.ShowFields("name.first");
            characterJoin.IsOuterJoin(false);

            CensusJoin onlineStatusJoin = characterJoin.JoinService("characters_online_status");
            onlineStatusJoin.WithInjectAt("online_status");
            onlineStatusJoin.ShowFields("online_status");
            onlineStatusJoin.IsOuterJoin(false);

            CensusJoin worldJoin = onlineStatusJoin.JoinService("world");
            worldJoin.OnField("online_status");
            worldJoin.ToField("world_id");
            worldJoin.WithInjectAt("ignore_this");
            worldJoin.IsOuterJoin(false);
            worldJoin.ShowFields("world_id");

            OnlineStatus onlineStatus = await query.GetAsync<OnlineStatus>().ConfigureAwait(false);

            //https://census.daybreakgames.com/get/ps2/outfit?alias_lower=r18&c:show=name,outfit_id&c:join=outfit_member%5Einject_at:members%5Eshow:character_id%5Eouter:0%5Elist:1(character%5Eshow:name.first%5Einject_at:character%5Eouter:0%5Eon:character_id(characters_online_status%5Einject_at:online_status%5Eshow:online_status%5Eouter:0(world%5Eon:online_status%5Eto:world_id%5Eouter:0%5Eshow:world_id%5Einject_at:ignore_this))
        }

        private Task OnCensusConnect(ReconnectionType type)
        {
            if (type == ReconnectionType.Initial)
                Log.Information("Census websocket client connected!!");
            else
                Log.Information("Census websocket client reconnected!!");

            _censusClient.Subscribe(_censusSubscription);

            return Task.CompletedTask;
        }

        private async Task OnCensusMessage(string message)
        {
            if (message == null)
                return;

            JToken msg;

            try
            {
                msg = JToken.Parse(message);
            }
            catch (Exception)
            {
                Log.Error("Failed to parse census message: {0}", message);
                return;
            }

            Log.Verbose($"Received census message: {msg}");
        }

        private Task OnCensusDisconnect(DisconnectionInfo info)
        {
            Log.Information($"Census websocket client disconnected: {info.Type}");
            // TODO: We might have to attempt reconnection here?
            return Task.CompletedTask;
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            await _censusClient.DisconnectAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
        }
    }
}
