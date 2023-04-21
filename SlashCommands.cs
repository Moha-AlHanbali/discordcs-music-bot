
namespace MusicBot
{
    using DSharpPlus.SlashCommands;

    public class SlashCommands : ApplicationCommandModule
    {
        CommandsCore commandsCore;


        public SlashCommands(CommandsCore commandsCore)
        {
            this.commandsCore = commandsCore;
        }

        public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext context)
        {
            commandsCore.UpdateLatestActivity();
            return await base.BeforeSlashExecutionAsync(context);
        }

        [SlashCommand("join", "Join the bot to a voice channel")]
        public async Task JoinCommand(InteractionContext context)
        {
            await commandsCore.JoinCommand(context);
        }

        [SlashCommand("leave", "Remove the bot from voice channel")]
        public async Task LeaveCommand(InteractionContext context)
        {
            await commandsCore.LeaveCommand(context);
        }

        [SlashCommand("add", "Add a track/playlist to the track queue")]
        public async Task AddCommand(InteractionContext context, [Option("track", "Track title or Youtube URL")] string path)
        {
            await commandsCore.AddCommand(context, path);
        }


        [SlashCommand("play", "Add a track/playlist to the track queue and start playing")]
        public async Task PlayCommand(InteractionContext context, [Option("track", "Track title or Youtube URL")] string path)
        {
            await commandsCore.PlayCommand(context, path);
        }


        [SlashCommand("pause", "Pause the current track")]
        public async Task PauseCommand(InteractionContext context)

        {
            await commandsCore.PauseCommand(context);
        }

        [SlashCommand("resume", "Resume a paused track")]
        public async Task ResumeCommand(InteractionContext context)
        {
            await commandsCore.ResumeCommand(context);
        }

        [SlashCommand("stop", "Stop playing and clear track queue")]
        public async Task StopCommand(InteractionContext context)
        {
            await commandsCore.StopCommand(context);
        }

        [SlashCommand("queue", "List track queue")]
        public async Task QueueCommand(InteractionContext context)
        {
            await commandsCore.QueueCommand(context);
        }

        [SlashCommand("skip", "Skip current track")]
        public async Task SkipCommand(InteractionContext context)
        {
            await commandsCore.SkipCommand(context);
        }

        [SlashCommand("repeat", "Toggle loop playing current track")]
        public async Task RepeatCommand(InteractionContext context)
        {
            await commandsCore.RepeatCommand(context);
        }
        [SlashCommand("replay", "Restart current track")]
        public async Task ReplayCommand(InteractionContext context)
        {
            await commandsCore.ReplayCommand(context);
        }
        [SlashCommand("volume", "Set bot volume level")]
        public async Task VolumeCommand(InteractionContext context, [Option("volume", "Volume level between 0 and 100% inclusive")] long volume)
        {
            await commandsCore.VolumeCommand(context, volume);
        }
    }
}