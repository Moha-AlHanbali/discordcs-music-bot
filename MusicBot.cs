namespace MusicBot
{
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
                Intents = DiscordIntents.All,
                MessageCacheSize = 2048,
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
            Utils utils = new Utils();
            utils.PrepareMediaDirectory();
            
            await bot.ConnectAsync();
            await Task.Delay(-1);
        }
    }

}