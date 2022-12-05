namespace MusicBot
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
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

            var bot = new DiscordClient(new DiscordConfiguration()
            {
                Token = Environment.GetEnvironmentVariable("TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug

            });

            Console.WriteLine("ONLINE");

            var commands = bot.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" }
            });

            commands.RegisterCommands<BotCommands>();

            Console.WriteLine("REGISTERED COMMANDS");

            await bot.ConnectAsync();
            await Task.Delay(-1);
        }
    }

}