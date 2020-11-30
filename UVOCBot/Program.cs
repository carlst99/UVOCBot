﻿using DSharpPlus;
using DSharpPlus.Entities;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot
{
    public static class Program
    {
        // Permissions integer: 268504128
        // - Manage Roles
        // - Send Messages
        // - Read Message History
        // - Add Reactions
        // - View Channels
        // OAuth2 URL: https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=268504128&scope=bot

        /// <summary>
        /// The name of the environment variable storing our bot token
        /// </summary>
        private const string TOKEN_ENV_NAME = "UVOC_BOT_TOKEN";

        public const string PREFIX = "ub!";
        public const string NAME = "UVOCBot";

        public static DiscordClient Client { get; private set; }

        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            // Useful for debugging
            Log.Information("Appdata stored in " + GetAppdataFilePath(null));

            // Connect to the Discord API
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable(TOKEN_ENV_NAME, EnvironmentVariableTarget.Process),
                TokenType = TokenType.Bot,
                LoggerFactory = SetupLogging(),
                Intents = DiscordIntents.DirectMessageReactions
                | DiscordIntents.DirectMessages
                | DiscordIntents.GuildMessageReactions
                | DiscordIntents.GuildMessages
                | DiscordIntents.Guilds
                | DiscordIntents.GuildVoiceStates
            });
            await Client.ConnectAsync(new DiscordActivity(PREFIX + "help", ActivityType.ListeningTo)).ConfigureAwait(false);

            Client.MessageCreated += async (_, e) =>
            {
                if (e.Message.Content.StartsWith("ub!ping", StringComparison.OrdinalIgnoreCase))
                    await e.Message.RespondAsync(":construction: pong! :construction:").ConfigureAwait(false);
            };

            // Begin any scheduled tasks we have
            JobManager.Initialize(new JobRegistry());
            JobManager.JobException += info => Log.Error(info.Exception, "An error occured in the job {name}", info.Name);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the path to the specified file, assuming that it is in our appdata store
        /// </summary>
        /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory</param>
        /// <remarks>Data is stored in the local appdata</remarks>
        /// <returns></returns>
        public static string GetAppdataFilePath(string fileName)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (fileName is not null)
                directory = Path.Combine(directory, "UVOCBot");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (fileName is not null)
                return Path.Combine(directory, fileName);
            else
                return directory;
        }

        private static ILoggerFactory SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
                .MinimumLevel.Override("DSharpPlus", Serilog.Events.LogEventLevel.Information)
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.Console()
                .CreateLogger();

            return new LoggerFactory().AddSerilog();
        }
    }
}
