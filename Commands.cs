
namespace MusicBot
{
    using System.IO;
    using System.Diagnostics;
    using DSharpPlus.Entities;
    using DSharpPlus.VoiceNext;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using YoutubeExplode;
    using MusicBot;
    public class BotCommands : BaseCommandModule
    {
        Utils utils;
        Queue<Track> trackQueue;
        Boolean playStatus;
        Boolean skipFlag;
        Boolean repeatFlag;
        Boolean replayFlag;
        String botChannelResponse;
        String memberChannelResponse;

        public BotCommands(Utils utils, Queue<Track> trackQueue, MusicBot.Program.BotCommandsOptions options)
        {
            this.utils = utils;
            this.trackQueue = trackQueue;
            this.playStatus = options.playStatus;
            this.skipFlag = options.skipFlag;
            this.repeatFlag = options.repeatFlag;
            this.replayFlag = options.replayFlag;
            this.botChannelResponse = options.botChannelResponse;
            this.memberChannelResponse = options.memberChannelResponse;
        }


        [Command("join")]
        public async Task JoinCommand(CommandContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await context.RespondAsync(memberChannelResponse);
                    return;
                }

                DiscordChannel? memberChannel = memberConnection.Channel;

                if (botConnection == null)
                {
                    await context.RespondAsync($"Joining {memberChannel?.Name} . . .");
                    await memberChannel.ConnectAsync();
                    return;
                }

                if (botConnection != null && botChannel != null && memberChannel?.Id != botChannel.Id)
                {
                    await context.RespondAsync($"Moving to {memberChannel?.Name} . . .");
                    botConnection.Disconnect();
                    await memberChannel.ConnectAsync();
                    return;

                }

                await context.RespondAsync($"Already joined to {memberChannel?.Name} channel");
                return;
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
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await context.RespondAsync(memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await context.RespondAsync(botChannelResponse);
                    return;
                }

                playStatus = false;
                trackQueue.Clear();
                utils.ClearMediaDirectory();
                botConnection.Disconnect();
                return;
            }
            catch
            {
                await context.RespondAsync("Could not leave channel..");
            }
        }
        [Command("add")]
        public async Task AddCommand(CommandContext context, params string[] path)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            var youtube = new Youtube();
            var youtubeClient = new YoutubeClient();


            if (memberConnection == null)
            {
                await context.RespondAsync(memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await context.RespondAsync(botChannelResponse);
                return;
            }

            if (path.Length > 0)
            {
                string joinedPath = string.Join(" ", path);
                if (trackQueue.Any())
                {
                    await AddTrack(context, youtube, youtubeClient, joinedPath);
                    return;
                }
                else if (!trackQueue.Any() && playStatus == false)
                {
                    await AddTrack(context, youtube, youtubeClient, joinedPath);
                    await PlayNext(context, trackQueue.Peek());
                    return;
                }
            }

            async Task AddTrack(CommandContext context, Youtube youtube, YoutubeClient youtubeClient, String joinedPath)
            {
                if (joinedPath.StartsWith("https://www.youtube.com/"))
                {
                    if (joinedPath.StartsWith("https://www.youtube.com/playlist?"))
                    {
                        var playlist = await youtubeClient.Playlists.GetAsync(joinedPath);

                        var videoList = await youtube.YoutubeGrabList(youtubeClient, joinedPath);
                        await context.RespondAsync($"Fetching tracks from {playlist.Title}...");
                        foreach (var video in videoList)
                        {
                            Track track = new Track(video.Title, "", video.Duration);
                            string mediaPath = await youtube.YoutubeDownload(youtubeClient, video.Url, track);
                            track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                            trackQueue.Enqueue(track);
                        }
                        await context.RespondAsync($"Added {videoList.Count} tracks from {playlist.Title} to queue");
                        return;
                    }
                    else
                    {
                        var song = await youtube.YoutubeGrab(youtubeClient, joinedPath);
                        Track track = new Track(song.Title, "", song.Duration);
                        string mediaPath = await youtube.YoutubeDownload(youtubeClient, joinedPath, track);
                        track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                        trackQueue.Enqueue(track);
                        await context.RespondAsync($"Added {track.TrackName} to queue");
                        return;
                    }
                }
                else
                {
                    var song = await youtube.YoutubeSearch(youtubeClient, joinedPath);
                    Track track = new Track(song.Title, "", song.Duration);
                    string mediaPath = await youtube.YoutubeDownload(youtubeClient, song.Url, track);
                    track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                    trackQueue.Enqueue(track);
                    await context.RespondAsync($"Added {track.TrackName} to queue");
                    return;
                }
            }
        }


        [Command("play")]
        public async Task PlayCommand(CommandContext context, params string[] path)
        {
            // try
            // {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await context.RespondAsync(memberChannelResponse);
                return;
            }
            if (botChannel == null)
            {
                await context.RespondAsync(botChannelResponse);
                return;
            }
            if (path.Length > 0)
            {
                await AddCommand(context, path);
                if (!trackQueue.Any())
                {
                    await PlayNext(context, trackQueue.Peek());
                    return;
                }
                else if (trackQueue.Any() && playStatus == false)
                {
                    await PlayNext(context, trackQueue.Peek());
                    return;
                }
            }
            else
            {
                await context.RespondAsync("Please specify a track to play. . .");
                return;
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
            if (skipFlag)
            {
                utils.PurgeFile(track.TrackURL);
                await context.RespondAsync($"Skipped {trackQueue.Dequeue().TrackName}");
            }
            else if (!repeatFlag)
            {
                utils.PurgeFile(track.TrackURL);
                await context.RespondAsync($"Finished playing {trackQueue.Dequeue().TrackName}");
            }
            skipFlag = false;
            if (trackQueue.Any()) await PlayNext(context, trackQueue.Peek());
            else playStatus = false;
            repeatFlag = false;
            utils.PurgeFile(track.TrackURL);
            return;
        }

        private async Task PlayAudio(CommandContext context, string mediaPath)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            var transmit = botConnection.GetTransmitSink();
            if (!utils.CheckFile(mediaPath)) ConvertWEBMtoMP3(mediaPath);
            var pcm = ConvertAudioToPcm(mediaPath);
            await pcm.CopyToAsync(transmit);
            await pcm.DisposeAsync();
            return;
        }
        private void ConvertWEBMtoMP3(string mediaPath)
        {
            Process? ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{mediaPath}"" -vn -ab 128k -ar 48000 -y ""{mediaPath.Replace(".webm", ".mp3")}"" ",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
        }

        private Stream ConvertAudioToPcm(string mediaPath)
        {
            Process? ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{mediaPath}"" -ac 2 -f s16le -ar 48000 pipe:1",
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
            return;
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
            return;
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
            return;
        }
        [Command("pause")]
        public async Task PauseCommand(CommandContext context)

        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await context.RespondAsync(memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await context.RespondAsync(botChannelResponse);
                return;
            }

            string PID = GetPID();
            PauseFFMPEG(PID);
            await context.RespondAsync($"Paused {trackQueue.Peek().TrackName}");
            return;
        }

        [Command("resume")]
        public async Task ResumeCommand(CommandContext context)

        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await context.RespondAsync(memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await context.RespondAsync(botChannelResponse);
                return;
            }

            string PID = GetPID();
            ResumeFFMPEG(PID);
            await context.RespondAsync($"Resumed {trackQueue.Peek().TrackName}");
            return;
        }

        [Command("stop")]
        public async Task StopCommand(CommandContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await context.RespondAsync(memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await context.RespondAsync(botChannelResponse);
                    return;
                }

                playStatus = false;
                StopFFMPEG();
                trackQueue.Clear();
                utils.ClearMediaDirectory();
                await context.RespondAsync("Player stopped and cleared Queue. . .");
                return;
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
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await context.RespondAsync(memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await context.RespondAsync(botChannelResponse);
                    return;
                }

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
                    return;
                }
                else
                {
                    await context.RespondAsync("Track queue is empty..");
                    return;
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
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await context.RespondAsync(memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await context.RespondAsync(botChannelResponse);
                    return;
                }

                if (trackQueue.Any())
                {
                    StopFFMPEG();
                    skipFlag = true;
                    repeatFlag = false;
                    replayFlag = false;
                    if (trackQueue.Any() && playStatus == false)
                    {
                        await PlayNext(context, trackQueue.Peek());
                    }
                    else playStatus = false;
                    return;
                }
                else
                {
                    await context.RespondAsync("No tracks to skip..");
                    return;
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
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await context.RespondAsync(memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await context.RespondAsync(botChannelResponse);
                return;
            }


            if (trackQueue.Any())
            {
                Track track = trackQueue.Peek();
                if (!repeatFlag)
                {
                    repeatFlag = true;
                    await context.RespondAsync($"Repeating {track.TrackName} - {track.TrackDuration}");
                    return;
                }
                else
                {
                    repeatFlag = false;
                    await context.RespondAsync($"Stopped repeating {track.TrackName} - {track.TrackDuration}");
                    return;
                }
            }
            else
            {
                await context.RespondAsync("Track queue is empty..");
                return;
            }


        }
        [Command("replay")]
        public async Task ReplayCommand(CommandContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await context.RespondAsync(memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await context.RespondAsync(botChannelResponse);
                return;
            }

            if (trackQueue.Any())
            {
                Track track = trackQueue.Peek();
                if (!replayFlag)
                {
                    replayFlag = true;
                    StopFFMPEG();
                    await context.RespondAsync($"Replaying {track.TrackName} - {track.TrackDuration}");
                    return;
                }
                else
                {
                    replayFlag = false;
                    await context.RespondAsync($"Stopped replaying {track.TrackName} - {track.TrackDuration}");
                    return;
                }
            }
            else
            {
                await context.RespondAsync("Track queue is empty..");
                return;
            }

        }
    }
}