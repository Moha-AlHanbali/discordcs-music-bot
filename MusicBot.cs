namespace MusicBot
{
    using System.Collections.Generic;
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.VoiceNext;
    using Microsoft.Extensions.Logging;

    class Program
    {
        static void Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            MainAsync().GetAwaiter().GetResult();
        }

        internal static async Task MainAsync()
        {
            // Create a bot instance
            var bot = new DiscordClient(new DiscordConfiguration()
            {
                Token = Environment.GetEnvironmentVariable("TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            });

            Console.WriteLine("ONLINE");

            // Extend Commands
            var commands = bot.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" }
            });

            commands.RegisterCommands<BotCommands>();

            // Extend Voice Activities
            bot.UseVoiceNext();

            await bot.ConnectAsync();
            await Task.Delay(-1);
        }
    }

}