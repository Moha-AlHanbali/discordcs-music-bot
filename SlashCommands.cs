
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

        [SlashCommand("join", "A slash command that instructs the bot to join your voice channel.")]
        public async Task JoinCommand(InteractionContext context)
        {
            await commandsCore.JoinCommand(context);
        }

        [SlashCommand("leave", "A slash command that instructs the bot to leave its' current voice channel.")]
        public async Task LeaveCommand(InteractionContext context)
        {
            await commandsCore.LeaveCommand(context);
        }

        [SlashCommand("add", "A slash command that instructs the bot to add a specific track.")]
        public async Task AddCommand(InteractionContext context, [Option("track", "Track title or Youtube URL")] string path)
        {
            await commandsCore.AddCommand(context, path);
        }


        [SlashCommand("play", "A slash command that instructs the bot to play a specific track.")]
        public async Task PlayCommand(InteractionContext context, [Option("track", "Track title or Youtube URL")] string path)
        {
            await commandsCore.PlayCommand(context, path);
        }


        [SlashCommand("pause", "A slash command that instructs the bot to pause current track.")]
        public async Task PauseCommand(InteractionContext context)

        {
            await commandsCore.PauseCommand(context);
        }

        [SlashCommand("resume", "A slash command that instructs the bot to resume a paused track.")]
        public async Task ResumeCommand(InteractionContext context)
        {
            await commandsCore.ResumeCommand(context);
        }

        [SlashCommand("stop", "A slash command that instructs the bot to stop playing completely.")]
        public async Task StopCommand(InteractionContext context)
        {
            await commandsCore.StopCommand(context);
        }

        [SlashCommand("queue", "A slash command that instructs the bot to show track queue")]
        public async Task QueueCommand(InteractionContext context)
        {
            await commandsCore.QueueCommand(context);
        }

        [SlashCommand("skip", "A slash command that instructs the bot to skip current track.")]
        public async Task SkipCommand(InteractionContext context)
        {
            await commandsCore.SkipCommand(context);
        }

        [SlashCommand("repeat", "A slash command that instructs the bot to repeat/stop repeating current track.")]
        public async Task RepeatCommand(InteractionContext context)
        {
            await commandsCore.RepeatCommand(context);
        }
        [SlashCommand("replay", "A slash command that instructs the bot to repeat current track.")]
        public async Task ReplayCommand(InteractionContext context)
        {
            await commandsCore.ReplayCommand(context);
        }
        [SlashCommand("volume", "A slash command that instructs the bot to set the player volume.")]
        public async Task VolumeCommand(InteractionContext context, [Option("volume", "Volume level between 0 and 100% inclusive")] long volume)
        {
            await commandsCore.VolumeCommand(context, volume);
        }
    }
}