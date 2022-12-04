
namespace MusicBot
{
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;

    public class BotCommands : BaseCommandModule
    {
        [Command("play")]
        public async Task PlayCommand(CommandContext ctx)
        {
            Console.WriteLine("PLAY COMMAND INVOKED");
            await ctx.RespondAsync("Playing . . .");
        }
    }

}