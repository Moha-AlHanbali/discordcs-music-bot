
namespace MusicBot
{
    using DSharpPlus.Entities;
    using DSharpPlus.VoiceNext;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;

    public class BotCommands : BaseCommandModule
    {
        [Command("join")]
        public async Task JoinCommand(CommandContext context, DiscordChannel? channel = null)

        {
            try
            {
                channel ??= context.Member?.VoiceState?.Channel;
                await context.RespondAsync($"Joining {channel?.Name} . . .");
                await channel.ConnectAsync();
            }
            catch
            {
                await context.RespondAsync("Could not join channel..");
            }

        }

        [Command("leave")]
        public async Task LeaveCommand(CommandContext context, VoiceNextConnection? connection = null)
        {
            try
            {
                var voiceNext = context.Client.GetVoiceNext();
                connection ??= voiceNext?.GetConnection(context.Guild);
                if (connection != null)
                {
                    connection.Disconnect();
                }
                else
                {
                    await context.RespondAsync("Not joined to a VC..");
                }
            }
            catch
            {
                await context.RespondAsync("Could not leave channel..");

            }
        }

        [Command("play")]
        public async Task PlayCommand(CommandContext context)
        {
            await context.RespondAsync("Playing . . .");
        }
    }

}