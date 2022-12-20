
namespace MusicBot
{
    using System.IO;
    using System.Diagnostics;
    using DSharpPlus.Entities;
    using DSharpPlus.VoiceNext;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using YoutubeExplode;

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
            try
            {
                var youtube = new Youtube();
                var yt = new YoutubeClient();

                if (path.Length > 0)
                {
                    string joinedPath = string.Join(" ", path);

                    if (joinedPath.StartsWith("https://www.youtube.com/"))
                    {
                        var song = await youtube.YoutubeGrab(yt, joinedPath);
                        var streamURL = await youtube.YoutubeStream(yt, joinedPath);
                        await context.RespondAsync($"Playing {song.Title} - {song.Duration} ♪");
                        await PlayAudio(context, streamURL);
                        await context.RespondAsync($"Finished playing {song.Title} ");
                    }
                    else
                    {
                        var song = await youtube.YoutubeSearch(yt, joinedPath);
                        var streamURL = await youtube.YoutubeStream(yt, song.Url);
                        await context.RespondAsync($"Playing {song.Title} - {song.Duration} ♪");
                        await PlayAudio(context, streamURL);
                        await context.RespondAsync($"Finished playing {song.Title} ");
                    }

                }
                else
                {
                    await context.RespondAsync("Please specify a track to play. . .");
                }
            }
            catch
            {
                await context.RespondAsync($"Unable to play track...");
            }

        }
        private async Task PlayAudio(CommandContext context, string songURL)
        {
            var voiceNext = context.Client.GetVoiceNext();
            var connection = voiceNext.GetConnection(context.Guild);
            var transmit = connection.GetTransmitSink();
            var pcm = ConvertAudioToPcm(songURL);
            await pcm.CopyToAsync(transmit);
            await pcm.DisposeAsync();
        }

        private Stream ConvertAudioToPcm(string streamURL)
        {
            var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                // Arguments = $@"-i ""{filePath}"" -ac 2 -f s16le -ar 48000 pipe:1",
                Arguments = $@"-reconnect 1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2 -i {streamURL} -ac 2 -f s16le -ar 48000 pipe:1",

                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            return ffmpeg.StandardOutput.BaseStream;
        }


        [Command("pause")]
        public async Task PauseCommand(CommandContext context, VoiceNextConnection? connection = null)

        {
            try
            {
                var voiceNext = context.Client.GetVoiceNext();
                connection ??= voiceNext?.GetConnection(context.Guild);
                if (connection != null)
                {

                    connection.Pause();
                    await context.RespondAsync("Track paused. . .");

                }
                else
                {
                    await context.RespondAsync("Not joined to a channel..");
                }
            }
            catch
            {
                await context.RespondAsync("Could not pause track..");

            }

        }

        [Command("resume")]
        public async Task ResumeCommand(CommandContext context, VoiceNextConnection? connection = null)

        {
            try
            {
                var voiceNext = context.Client.GetVoiceNext();
                connection ??= voiceNext?.GetConnection(context.Guild);
                if (connection != null)
                {

                    await connection.ResumeAsync();
                    await context.RespondAsync("Track resumed. . .");

                }
                else
                {
                    await context.RespondAsync("Not joined to a channel..");
                }
            }
            catch
            {
                await context.RespondAsync("Could not resume track..");

            }

        }

        [Command("stop")]
        public async Task StopCommand(CommandContext context, VoiceNextConnection? connection = null)

        {
            try
            {
                var voiceNext = context.Client.GetVoiceNext();
                connection ??= voiceNext?.GetConnection(context.Guild);

                if (connection != null)
                {
                    connection.Disconnect();

                    await context.RespondAsync("Player stopped. . .");

                }
                else
                {
                    await context.RespondAsync("Not joined to a channel..");
                }
            }
            catch
            {
                await context.RespondAsync("Could not stop player..");

            }

        }
    }
}
