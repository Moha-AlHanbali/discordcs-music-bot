namespace MusicBot
{

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using Microsoft.Extensions.Logging;

    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = Environment.GetEnvironmentVariable("TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug

            });
            Console.WriteLine("ONLINE");

            discord.MessageCreated += async (s, e) =>
                      {
                        Console.WriteLine("READING");
                          if (e.Message.Content.ToLower().StartsWith("ping")){
                            Console.WriteLine("PINGING");
                            await e.Message.RespondAsync("pong!");
                          }
                      };

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" }
            });

            commands.RegisterCommands<BotCommands>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }

}