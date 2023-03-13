
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
        Queue<Track> trackQueue = new Queue<Track>();

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
                    trackQueue.Clear();
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
        [Command("add")]
        public async Task AddCommand(CommandContext context, params string[] path)
        {
            var youtube = new Youtube();
            var youtubeClient = new YoutubeClient();

            if (path.Length > 0)
            {
                string joinedPath = string.Join(" ", path);
                if (trackQueue.Any())
                {
                    await AddTrack(context, youtube, youtubeClient, joinedPath);
                }
                else
                {
                    await AddTrack(context, youtube, youtubeClient, joinedPath);
                    await PlayNext(context, trackQueue.Peek());
                }

            }
            else
            {
                await context.RespondAsync("Please specify a track to add. . .");
            }
            async Task AddTrack(CommandContext context, Youtube youtube, YoutubeClient youtubeClient, String joinedPath)
            {
                if (joinedPath.StartsWith("https://www.youtube.com/"))
                {
                    Track track = await GrabYoutubeURL(youtube, youtubeClient, joinedPath);
                    await context.RespondAsync($"Added {track.TrackName} to queue");
                }
                else
                {
                    Track track = await SearchYoutube(youtube, youtubeClient, joinedPath);
                    await context.RespondAsync($"Added {track.TrackName} to queue");
                }
            }
            async Task<Track> GrabYoutubeURL(Youtube youtube, YoutubeClient youtubeClient, String joinedPath)
            {
                var song = await youtube.YoutubeGrab(youtubeClient, joinedPath);
                var streamURL = await youtube.YoutubeStream(youtubeClient, joinedPath);
                Track track = new Track(song.Title, streamURL, song.Duration);
                trackQueue.Enqueue(track);
                return track;
            }

            async Task<Track> SearchYoutube(Youtube youtube, YoutubeClient youtubeClient, String joinedPath)
            {
                var song = await youtube.YoutubeSearch(youtubeClient, joinedPath);
                var streamURL = await youtube.YoutubeStream(youtubeClient, song.Url);
                Track track = new Track(song.Title, streamURL, song.Duration);
                trackQueue.Enqueue(track);
                return track;
            }
        }

        [Command("play")]
        public async Task PlayCommand(CommandContext context, params string[] path)
        {
            // try
            // {

            if (path.Length > 0)
            {
                await AddCommand(context, path);
                if (!trackQueue.Any()) await PlayNext(context, trackQueue.Peek());
            }
            else
            {
                await context.RespondAsync("Please specify a track to play. . .");
            }
            // }
            // catch
            // {
            //     await context.RespondAsync($"Unable to play track...");
            // }

        }
        private async Task PlayNext(CommandContext context, Track track)
        {
            await context.RespondAsync($"Playing {track.TrackName} - {track.TrackDuration} ♪");
            await PlayAudio(context, track.TrackURL);
            await context.RespondAsync($"Finished playing {trackQueue.Dequeue().TrackName}");
            if (trackQueue.Any()) await PlayNext(context, trackQueue.Peek());
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

        private string GetPID()
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.FileName = "pgrep";
            proc.StartInfo.Arguments = "ffmpeg";
            proc.Start();
            string PID = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return PID;
        }

        private void PauseFFMPEG(string PID)
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.FileName = "kill";
            proc.StartInfo.Arguments = $@"-s SIGSTOP {Int32.Parse(PID)}";
            proc.Start();
            proc.WaitForExit();
        }

        private void ResumeFFMPEG(string PID)
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.FileName = "kill";
            proc.StartInfo.Arguments = $@"-s SIGCONT {Int32.Parse(PID)}";
            proc.Start();
            proc.WaitForExit();
        }

        [Command("pause")]
        public async Task PauseCommand(CommandContext context, VoiceNextConnection? connection = null)

        {
            // try
            // {
            var voiceNext = context.Client.GetVoiceNext();
            connection ??= voiceNext?.GetConnection(context.Guild);
            if (connection != null)
            {
                string PID = GetPID();
                PauseFFMPEG(PID);
                // connection.Pause();
                Track track = trackQueue.Peek();
                await context.RespondAsync($"Paused {track.TrackName}");

            }
            else
            {
                await context.RespondAsync("Not joined to a channel..");
            }
            // }
            // catch
            // {
            //     await context.RespondAsync("Could not pause track..");

            // }

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
                    string PID = GetPID();
                    ResumeFFMPEG(PID);
                    Track track = trackQueue.Peek();

                    // await connection.ResumeAsync();
                    await context.RespondAsync($"Resumed {track.TrackName}");

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
                    trackQueue.Clear();
                    connection.Disconnect();

                    await context.RespondAsync("Player stopped and cleared Queue. . .");

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

        [Command("queue")]
        public async Task QueueCommand(CommandContext context, VoiceNextConnection? connection = null)

        {
            try
            {
                var voiceNext = context.Client.GetVoiceNext();
                connection ??= voiceNext?.GetConnection(context.Guild);

                if (connection != null)
                {
                    if (trackQueue.Any())
                    {
                        string message = "Track Queue:\n";
                        Int16 counter = 1;
                        foreach (Track track in trackQueue.ToArray())
                        {
                            message += $"{counter}. {track.TrackName} - {track.TrackDuration}\n";
                            counter++;
                        }
                        await context.RespondAsync(message);

                    }
                    else
                    {
                        await context.RespondAsync("Track queue is empty..");
                    }


                }
                else
                {
                    await context.RespondAsync("Not joined to a channel..");
                }
            }
            catch
            {
                await context.RespondAsync("Could not show queue..");

            }

        }
    }
}
