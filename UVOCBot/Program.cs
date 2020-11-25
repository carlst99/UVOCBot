using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;

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

        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            DiscordClient client = new DiscordClient(new DiscordConfiguration
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
            await client.ConnectAsync(new DiscordActivity(PREFIX + "help", ActivityType.ListeningTo)).ConfigureAwait(false);

            client.MessageCreated += async (_, e) =>
            {
                if (e.Message.Content.StartsWith("ub!ping", StringComparison.OrdinalIgnoreCase))
                    await e.Message.RespondAsync("pong!").ConfigureAwait(false);
            };

            await Task.Delay(-1).ConfigureAwait(false);
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
