namespace MusicBot
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.VoiceNext;
    using DSharpPlus.SlashCommands;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public class Program
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

            // Dependency Injection
            // var services = new ServiceCollection()
            // .AddSingleton(typeof(Boolean), false)
            // .AddSingleton(typeof(String), "")
            // .AddSingleton(typeof(Utils), new Utils())
            // .AddSingleton(typeof(Queue<Track>), new Queue<Track>())
            // .BuildServiceProvider();

            Queue<Track> botTrackQueue = new Queue<Track>();
            Utils botUtils = new Utils();
            CommandsCore commandsCore = new CommandsCore(botUtils, botTrackQueue, new BotCommandsOptions());


            var commandsServices = new ServiceCollection()
                .AddSingleton(typeof(CommandsCore), commandsCore)
                .BuildServiceProvider();

            // Extend Commands
            var commands = bot.UseCommandsNext(new CommandsNextConfiguration()
            {
                Services = commandsServices,
                StringPrefixes = new[] { "!" }
            });
            commands.RegisterCommands<BotCommands>();

            var slashCommands = bot.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = commandsServices,
            });
            slashCommands.RegisterCommands<SlashCommands>();

            // Extend Voice Activities
            bot.UseVoiceNext();
            Utils utils = new Utils();
            utils.PrepareMediaDirectory();

            await bot.ConnectAsync();
            await Task.Delay(-1);
        }
        public class BotCommandsOptions
        {
            public bool playStatus { get; set; } = false;
            public bool skipFlag { get; set; } = false;
            public bool repeatFlag { get; set; } = false;
            public bool replayFlag { get; set; } = false;
            public string botChannelResponse { get; set; } = "Bot must join a voice channel to accept commands";
            public string memberChannelResponse { get; set; } = "You have to be in a voice channel to use bot commands";
        }
    }

}