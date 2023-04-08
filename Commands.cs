
namespace MusicBot
{
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;

    public class BotCommands : BaseCommandModule
    {
        CommandsCore commandsCore;

        public BotCommands(CommandsCore commandsCore)
        {
            this.commandsCore = commandsCore;
        }


        [Command("join")]
        public async Task JoinCommand(CommandContext context)
        {
            await commandsCore.JoinCommand(context);
        }

        [Command("leave")]
        public async Task LeaveCommand(CommandContext context)
        {
            await commandsCore.LeaveCommand(context);
        }

        [Command("add")]
        public async Task AddCommand(CommandContext context, params string[] path)
        {
            await commandsCore.AddCommand(context, path);
        }


        [Command("play")]
        public async Task PlayCommand(CommandContext context, params string[] path)
        {
            await commandsCore.PlayCommand(context, path);
        }


        [Command("pause")]
        public async Task PauseCommand(CommandContext context)

        {
            await commandsCore.PauseCommand(context);
        }

        [Command("resume")]
        public async Task ResumeCommand(CommandContext context)
        {
            await commandsCore.ResumeCommand(context);
        }

        [Command("stop")]
        public async Task StopCommand(CommandContext context)
        {
            await commandsCore.StopCommand(context);
        }

        [Command("queue")]
        public async Task QueueCommand(CommandContext context)
        {
            await commandsCore.QueueCommand(context);
        }

        [Command("skip")]
        public async Task SkipCommand(CommandContext context)
        {
            await commandsCore.SkipCommand(context);
        }

        [Command("repeat")]
        public async Task RepeatCommand(CommandContext context)
        {
            await commandsCore.RepeatCommand(context);
        }
        [Command("replay")]
        public async Task ReplayCommand(CommandContext context)
        {
            await commandsCore.ReplayCommand(context);
        }
        [Command("volume")]
        public async Task VolumeCommand(CommandContext context, int volume)
        {
            await commandsCore.VolumeCommand(context, volume);
        }
    }
}