using DaybreakGames.Census.Stream;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace UVOCBot.Workers
{
    public sealed class PlanetsideWorker : BackgroundService
    {
        private readonly ICensusStreamClient _censusClient;

        private readonly CensusStreamSubscription _censusSubscription = new CensusStreamSubscription
        {
            Worlds = new[] { "all" },
            EventNames = new[] { "ContinentLock", "ContinentUnlock", "MetagameEvent" }
        };

        public PlanetsideWorker(ICensusStreamClient censusClient, IHostApplicationLifetime appLifetime)
        {
            _censusClient = censusClient;
            _censusClient.OnConnect(OnCensusConnect);
            _censusClient.OnMessage(OnCensusMessage);
            _censusClient.OnDisconnect(OnCensusDisconnect);

            appLifetime.ApplicationStopping.Register(OnStopping);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _censusClient.ConnectAsync().ConfigureAwait(false);
            await Task.Delay(-1, stoppingToken).ConfigureAwait(false);
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

            Log.Information($"Received census message: {msg}");
        }

        private Task OnCensusDisconnect(DisconnectionInfo info)
        {
            Log.Information($"Census websocket client disconnected: {info.Type}");
            // TODO: We might have to attempt reconnection here
            return Task.CompletedTask;
        }

        private void OnStopping()
        {
            _censusClient.DisconnectAsync().Wait();
        }
    }
}
