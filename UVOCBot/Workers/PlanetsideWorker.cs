using DaybreakGames.Census;
using DaybreakGames.Census.Stream;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Services.Abstractions;
using Websocket.Client;

namespace UVOCBot.Workers
{
    public sealed class PlanetsideWorker : BackgroundService
    {
        private readonly ICensusStreamClient _censusClient;
        private readonly ICensusQueryFactory _censusQueryFactory;
        private readonly IDbApiService _dbApi;

        private readonly CensusStreamSubscription _censusSubscription = new()
        {
            Worlds = new[] { "all" },
            EventNames = new[] { "ContinentLock", "ContinentUnlock", "MetagameEvent" }
        };

        public PlanetsideWorker(ICensusStreamClient censusClient, ICensusQueryFactory censusQueryFactory, IDbApiService dbApi)
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

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(300000, stoppingToken).ConfigureAwait(false); // Work every 5min
            }
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
