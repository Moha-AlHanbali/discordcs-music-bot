
namespace MusicBot
{
    using System.Text.RegularExpressions;
    using System.IO;
    using System.Diagnostics;
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
                    await context.RespondAsync("Not joined to a channel..");
                }
            }
            catch
            {
                await context.RespondAsync("Could not leave channel..");

            }
        }

        [Command("play")]
        public async Task PlayCommand(CommandContext context, params string[] path)
        {
            string joinedPath = string.Join(" ", path);
            Console.WriteLine(joinedPath);
            if (joinedPath.Trim() != "" && joinedPath.Trim() != null)
            {
                // string trimmedPath = Regex.Replace(path, " ", "20%");
                Console.WriteLine($"Started playing {joinedPath}");
                var voiceNext = context.Client.GetVoiceNext();
                var connection = voiceNext.GetConnection(context.Guild);
                var transmit = connection.GetTransmitSink();
                var pcm = ConvertAudioToPcm(joinedPath);
                await pcm.CopyToAsync(transmit);
                await pcm.DisposeAsync();
                await context.RespondAsync($"Playing {joinedPath}");
            }
            else
            {
                await context.RespondAsync("Please specify a track to play. . .");
            }

        }
        private Stream ConvertAudioToPcm(string filePath)
        {
            var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{filePath}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            return ffmpeg.StandardOutput.BaseStream;
        }

    }

}