
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
        Boolean playStatus = false;
        Boolean skipFlag = false;
        Boolean repeatFlag = false;
        Boolean replayFlag = false;

        [Command("join")]
        public async Task JoinCommand(CommandContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? memberChannel = context.Member?.VoiceState?.Channel;
                DiscordChannel? botChannel = botConnection?.TargetChannel;

                if (memberChannel != null)
                {
                    if (botChannel == null)
                    {
                        await context.RespondAsync($"Joining {memberChannel?.Name} . . .");
                        await memberChannel.ConnectAsync();
                    }
                    else if (botConnection != null && memberChannel?.Id != botChannel.Id)
                    {
                        await context.RespondAsync($"Joining {memberChannel?.Name} . . .");
                        botConnection.Disconnect();
                        await memberChannel.ConnectAsync();
                    }
                    else
                    {
                        await context.RespondAsync($"Already joined to {memberChannel?.Name} channel");
                    }
                }
                else
                {
                    await context.RespondAsync("Not joined to a channel..");

                }
            }
            catch
            {
                await context.RespondAsync("Could not join channel..");
            }
        }

        private VoiceNextConnection GetBotConnection(CommandContext context)
        {
            var voiceNext = context.Client.GetVoiceNext();
            return voiceNext.GetConnection(context.Guild);
        }

        [Command("leave")]
        public async Task LeaveCommand(CommandContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                if (botConnection != null)
                {
                    playStatus = false;
                    trackQueue.Clear();
                    botConnection.Disconnect();
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

            VoiceNextConnection botConnection = GetBotConnection(context);

            if (botConnection != null)
            {
                if (path.Length > 0)
                {
                    string joinedPath = string.Join(" ", path);
                    if (trackQueue.Any())
                    {
                        await AddTrack(context, youtube, youtubeClient, joinedPath);
                    }
                    else if (!trackQueue.Any() && playStatus == false)
                    {
                        playStatus = true;
                        await AddTrack(context, youtube, youtubeClient, joinedPath);
                        await PlayNext(context, trackQueue.Peek());
                    }
                }
                else
                {
                    await context.RespondAsync("Please specify a track to add. . .");
                }
            }
            else
            {
                await context.RespondAsync("Not joined to a channel..");
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
            VoiceNextConnection botConnection = GetBotConnection(context);

            if (botConnection != null)
            {
                if (path.Length > 0)
                {
                    await AddCommand(context, path);
                    if (!trackQueue.Any())
                    {
                        playStatus = true;
                        await PlayNext(context, trackQueue.Peek());
                    }
                    else if (trackQueue.Any() && playStatus == false)
                    {
                        playStatus = true;
                        await PlayNext(context, trackQueue.Peek());
                    }
                }
                else
                {
                    await context.RespondAsync("Please specify a track to play. . .");
                }
            }
            else
            {
                await context.RespondAsync("Not joined to a channel..");
            }
            // }
            // catch
            // {
            //     await context.RespondAsync($"Unable to play track...");
            // }

        }
        private async Task PlayNext(CommandContext context, Track track)
        {
            playStatus = true;
            if (!repeatFlag) await context.RespondAsync($"Playing {track.TrackName} - {track.TrackDuration} ♪");
            await PlayAudio(context, track.TrackURL);
            if (replayFlag)
            {
                replayFlag = false;
                await PlayNext(context, trackQueue.Peek());
                return;
            }
            if (skipFlag) await context.RespondAsync($"Skipped {trackQueue.Dequeue().TrackName}");
            else if (!repeatFlag) await context.RespondAsync($"Finished playing {trackQueue.Dequeue().TrackName}");
            skipFlag = false;
            if (trackQueue.Any()) await PlayNext(context, trackQueue.Peek());
            else playStatus = false;
            repeatFlag = false;
        }


        private async Task PlayAudio(CommandContext context, string songURL)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            var transmit = botConnection.GetTransmitSink();
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

        private void StopFFMPEG()
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.FileName = "killall";
            proc.StartInfo.Arguments = $@"ffmpeg";
            proc.Start();
            proc.WaitForExit();
        }
        [Command("pause")]
        public async Task PauseCommand(CommandContext context)

        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            if (botConnection != null)
            {
                string PID = GetPID();
                PauseFFMPEG(PID);
                await context.RespondAsync($"Paused {trackQueue.Peek().TrackName}");
            }
            else
            {
                await context.RespondAsync("Not joined to a channel..");
            }
        }

        [Command("resume")]
        public async Task ResumeCommand(CommandContext context)

        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            if (botConnection != null)
            {
                string PID = GetPID();
                ResumeFFMPEG(PID);
                await context.RespondAsync($"Resumed {trackQueue.Peek().TrackName}");

            }
            else
            {
                await context.RespondAsync("Not joined to a channel..");
            }
        }

        [Command("stop")]
        public async Task StopCommand(CommandContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);

                if (botConnection != null)
                {
                    playStatus = false;
                    StopFFMPEG();
                    trackQueue.Clear();
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
        public async Task QueueCommand(CommandContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);

                if (botConnection != null)
                {
                    if (trackQueue.Any())
                    {
                        string message = "Track Queue:\n";
                        Int16 counter = 1;
                        foreach (Track track in trackQueue.ToArray())
                        {
                            if (repeatFlag && counter == 1)
                            {
                                message += $"{counter}. {track.TrackName} - {track.TrackDuration} - Repeating\n";
                            }
                            else
                            {
                                message += $"{counter}. {track.TrackName} - {track.TrackDuration}\n";
                            }
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

        [Command("skip")]
        public async Task SkipCommand(CommandContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);

                if (botConnection != null)
                {
                    if (trackQueue.Any())
                    {
                        StopFFMPEG();
                        skipFlag = true;
                        repeatFlag = false;
                        replayFlag = false;
                        if (trackQueue.Any() && playStatus == false)
                        {
                            playStatus = true;
                            await PlayNext(context, trackQueue.Peek());
                        }
                        else playStatus = false;
                    }
                    else
                    {
                        await context.RespondAsync("No tracks to skip..");

                    }
                }
                else
                {
                    await context.RespondAsync("Not joined to a channel..");
                }

            }
            catch
            {
                await context.RespondAsync("Could not skip track..");

            }
        }

        [Command("repeat")]
        public async Task RepeatCommand(CommandContext context)
        {

            VoiceNextConnection botConnection = GetBotConnection(context);
            if (botConnection != null)
            {
                if (trackQueue.Any())
                {
                    Track track = trackQueue.Peek();
                    if (!repeatFlag)
                    {
                        repeatFlag = true;
                        await context.RespondAsync($"Repeating {track.TrackName} - {track.TrackDuration}");
                    }
                    else
                    {
                        repeatFlag = false;
                        await context.RespondAsync($"Stopped repeating {track.TrackName} - {track.TrackDuration}");

                    }
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
        [Command("replay")]
        public async Task ReplayCommand(CommandContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);


            if (botConnection != null)
            {
                if (trackQueue.Any())
                {
                    Track track = trackQueue.Peek();
                    if (!replayFlag)
                    {
                        replayFlag = true;
                        StopFFMPEG();
                        await context.RespondAsync($"Replaying {track.TrackName} - {track.TrackDuration}");

                    }
                    else
                    {
                        replayFlag = false;
                        await context.RespondAsync($"Stopped replaying {track.TrackName} - {track.TrackDuration}");

                    }
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
    }
}