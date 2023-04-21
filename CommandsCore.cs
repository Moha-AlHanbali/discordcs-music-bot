// NOTE: This module requires a heavy refactor.
// Since CommandContext and InteractionContext do not inherit from the same BaseContext, a wrapper class may be needed for aggregation.
// Methods are overloading for the time being instead. 

namespace MusicBot
{
    using System.IO;
    using System.Diagnostics;
    using DSharpPlus.Entities;
    using DSharpPlus.VoiceNext;
    using DSharpPlus.CommandsNext;
    using YoutubeExplode;
    using DSharpPlus.SlashCommands;
    using DSharpPlus;

    public class CommandsCore
    {

        Utils utils;
        Queue<Track> trackQueue;
        Boolean playStatus;
        Boolean skipFlag;
        Boolean repeatFlag;
        Boolean replayFlag;
        String botChannelResponse;
        String memberChannelResponse;
        Timer inactivityTimer;
        Int32 ffmpegProcess;
        public DateTime latestActivity;

        public CommandsCore(Utils utils, Queue<Track> trackQueue, Timer inactivityTimer, DateTime latestActivity, Int32 ffmpegProcess, MusicBot.Program.BotCommandsOptions options)
        {
            this.utils = utils;
            this.trackQueue = trackQueue;
            this.inactivityTimer = inactivityTimer;
            this.latestActivity = latestActivity;
            this.ffmpegProcess = ffmpegProcess;
            this.playStatus = options.playStatus;
            this.skipFlag = options.skipFlag;
            this.repeatFlag = options.repeatFlag;
            this.replayFlag = options.replayFlag;
            this.botChannelResponse = options.botChannelResponse;
            this.memberChannelResponse = options.memberChannelResponse;
        }

        #region Common Methods
        /// <summary>
        /// Converts a WEBM media file to an MP3 file using FFmpeg.
        /// </summary>
        /// <param name="mediaPath">The path to the WEBM media file.</param>
        private void ConvertWEBMtoACC(string mediaPath)
        {
            using (var ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = "ffmpeg";
                ffmpeg.StartInfo.Arguments = $@"-i ""{mediaPath}"" -vn -c:a aac -b:a 128k -ar 48000 -y ""{mediaPath.Replace(".webm", ".m4a")}"" ";
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.Start();

                // Wait for the process to complete before exiting the method.
                ffmpeg.WaitForExit();
            }
        }


        /// <summary>
        /// Converts an audio file to PCM format using FFmpeg.
        /// </summary>
        /// <param name="mediaPath">The path to the audio file.</param>
        /// <returns>A <see cref="Stream"/> containing the converted audio data in PCM format.</returns>
        private Stream ConvertAudioToPcm(string mediaPath)
        {
            using (Process ffmpeg = new Process())
            {
                // Start a new FFmpeg process to perform the conversion.
                ffmpeg.StartInfo.FileName = "ffmpeg";
                ffmpeg.StartInfo.Arguments = $@"-i ""{mediaPath}"" -ac 2 -f s16le -ar 48000 -nostdin -threads {Environment.ProcessorCount} pipe:1";
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.Start();

                ffmpegProcess = ffmpeg.Id;
                // Return the process's standard output stream, which contains the converted audio data.
                return ffmpeg.StandardOutput.BaseStream;
            }
        }


        /// <summary>
        /// Gets the process ID (PID) of the FFmpeg process.
        /// </summary>
        /// <returns>A string representing the PID of the FFmpeg process.</returns>
        private string GetPID()
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "pgrep";
                proc.StartInfo.Arguments = "ffmpeg";
                proc.Start();

                // Read the output of the process, which contains the FFmpeg process ID.
                string PID = proc.StandardOutput.ReadToEnd();

                // Wait for the process to exit before returning the PID.
                proc.WaitForExit();

                return PID;
            }
        }


        /// <summary>
        /// Pauses the FFmpeg process with the specified process ID (PID).
        /// </summary>
        /// <param name="PID">The process ID (PID) of the FFmpeg process to pause.</param>
        private void PauseFFMPEG(int PID)
        {
            // Start a new process to execute the kill command with the SIGSTOP signal and the specified PID.
            using (var proc = new Process())
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "kill";
                proc.StartInfo.Arguments = $@"-s SIGSTOP {PID}";
                proc.Start();

                // Wait for the process to exit before returning.
                proc.WaitForExit();
            }
        }



        /// <summary>
        /// Resumes the FFmpeg process with the specified process ID (PID).
        /// </summary>
        /// <param name="PID">The process ID (PID) of the FFmpeg process to resume.</param>
        private void ResumeFFMPEG(int PID)
        {
            // Start a new process to execute the kill command with the SIGCONT signal and the specified PID.
            using (var proc = new Process())
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "kill";
                proc.StartInfo.Arguments = $@"-s SIGCONT {PID}";
                proc.Start();

                // Wait for the process to exit before returning.
                proc.WaitForExit();
            }
        }


        /// <summary>
        /// Stops all running FFmpeg processes.
        /// </summary>
        private void StopFFMPEG()
        {
            // Start a new process to execute the killall command with the ffmpeg argument.
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.FileName = "killall";
            proc.StartInfo.Arguments = $@"ffmpeg";
            proc.Start();

            // Wait for the process to exit before returning.
            proc.WaitForExit();
            return;
        }

        public void UpdateLatestActivity()
        {
            latestActivity = DateTime.Now;
            return;
        }


        #endregion

        #region Command Helpers
        /// <summary>
        /// Sends a message in response to a command.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ReplyToCommand(CommandContext context, string message)
        {
            await context.RespondAsync(message);
            return;
        }

        /// <summary>
        /// Sends a message in response to an interaction.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ReplyToCommand(InteractionContext context, string message)
        {
            await context.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(message));
            return;
        }

        private async Task InitiateSlashCommand(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            return;
        }
        private async Task EndSlashCommand(InteractionContext context, String message)
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message));
            return;
        }
        private async Task FollowUpOnSlashCommand(InteractionContext context, String message)
        {
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(message));
            return;
        }
        /// <summary>
        /// Returns the VoiceNext connection for the current guild in a CommandContext.
        /// </summary>
        /// <param name="context">The CommandContext for which to get the VoiceNext connection.</param>
        /// <returns>The VoiceNext connection for the current guild in the CommandContext.</returns>
        private VoiceNextConnection GetBotConnection(CommandContext context)
        {
            var voiceNext = context.Client.GetVoiceNext();
            return voiceNext.GetConnection(context.Guild);
        }

        /// <summary>
        /// Returns the VoiceNext connection for the current guild in an InteractionContext.
        /// </summary>
        /// <param name="context">The InteractionContext for which to get the VoiceNext connection.</param>
        /// <returns>The VoiceNext connection for the current guild in the InteractionContext.</returns>
        private VoiceNextConnection GetBotConnection(InteractionContext context)
        {
            var voiceNext = context.Client.GetVoiceNext();
            return voiceNext.GetConnection(context.Guild);
        }

        /// <summary>
        /// Adds a track to the track queue from a YouTube URL or search query
        /// </summary>
        /// <param name="context">The Discord command context</param>
        /// <param name="youtube">The YouTube service instance</param>
        /// <param name="youtubeClient">The YouTube API client</param>
        /// <param name="joinedPath">The YouTube URL or search query</param>
        async Task AddTrack(CommandContext context, Youtube youtube, YoutubeClient youtubeClient, String joinedPath)
        {
            if (joinedPath.StartsWith("https://www.youtube.com/"))
            {
                if (joinedPath.StartsWith("https://www.youtube.com/playlist?"))
                {
                    var playlist = await youtubeClient.Playlists.GetAsync(joinedPath);

                    var videoList = await youtube.YoutubeGrabList(youtubeClient, joinedPath);
                    await ReplyToCommand(context, $"Processing tracks from {playlist.Title} playlist.");
                    foreach (var video in videoList)
                    {
                        Track track = new Track(video.Title, "", video.Duration);
                        string mediaPath = await youtube.YoutubeDownload(youtubeClient, video.Url, track);
                        track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                        trackQueue.Enqueue(track);
                    }
                    await ReplyToCommand(context, $"Added {videoList.Count} tracks from {playlist.Title} to queue");
                    return;
                }
                else
                {
                    var song = await youtube.YoutubeGrab(youtubeClient, joinedPath);
                    Track track = new Track(song.Title, "", song.Duration);
                    string mediaPath = await youtube.YoutubeDownload(youtubeClient, joinedPath, track);
                    track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                    trackQueue.Enqueue(track);
                    await ReplyToCommand(context, $"Added {track.TrackName} - {track.TrackDuration} to queue");
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
                await ReplyToCommand(context, $"Added {track.TrackName} - {track.TrackDuration} to queue");
                return;
            }
        }

        /// <summary>
        /// Adds a track to the track queue from a YouTube URL or search query
        /// </summary>
        /// <param name="context">The Discord interaction context</param>
        /// <param name="youtube">The YouTube service instance</param>
        /// <param name="youtubeClient">The YouTube API client</param>
        /// <param name="joinedPath">The YouTube URL or search query</param>
        async Task AddTrack(InteractionContext context, Youtube youtube, YoutubeClient youtubeClient, String joinedPath)
        {
            await InitiateSlashCommand(context);
            if (joinedPath.StartsWith("https://www.youtube.com/"))
            {
                if (joinedPath.StartsWith("https://www.youtube.com/playlist?"))
                {
                    var playlist = await youtubeClient.Playlists.GetAsync(joinedPath);

                    var videoList = await youtube.YoutubeGrabList(youtubeClient, joinedPath);
                    await FollowUpOnSlashCommand(context, $"Processing tracks from {playlist.Title} playlist");
                    foreach (var video in videoList)
                    {
                        Track track = new Track(video.Title, "", video.Duration);
                        string mediaPath = await youtube.YoutubeDownload(youtubeClient, video.Url, track);
                        track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                        trackQueue.Enqueue(track);
                    }
                    await FollowUpOnSlashCommand(context, $"Added {videoList.Count} tracks from {playlist.Title} to queue");
                    return;
                }
                else
                {
                    var song = await youtube.YoutubeGrab(youtubeClient, joinedPath);
                    Track track = new Track(song.Title, "", song.Duration);
                    string mediaPath = await youtube.YoutubeDownload(youtubeClient, joinedPath, track);
                    track.TrackURL = $"{System.IO.Directory.GetCurrentDirectory()}/{mediaPath}";
                    trackQueue.Enqueue(track);
                    await FollowUpOnSlashCommand(context, $"Added {track.TrackName} - {track.TrackDuration} to queue");
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
                await FollowUpOnSlashCommand(context, $"Added {track.TrackName} - {track.TrackDuration} to queue");
                return;
            }
        }

        /// <summary>
        /// Plays the next track in the queue and handles repeat, skip, and replay flags.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="track">The track to play.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PlayNext(CommandContext context, Track track)
        {
            playStatus = true;
            if (!repeatFlag) await ReplyToCommand(context, $"Playing {track.TrackName} - {track.TrackDuration} ♪");
            await PlayAudio(context, track.TrackURL);

            if (replayFlag)
            {
                replayFlag = false;
                UpdateLatestActivity();
                await PlayNext(context, trackQueue.Peek());
                return;
            }
            if (skipFlag)
            {
                utils.PurgeFile(track.TrackURL);
                await ReplyToCommand(context, $"Skipped {trackQueue.Dequeue().TrackName}");
            }
            else if (!repeatFlag)
            {
                utils.PurgeFile(track.TrackURL);
                await ReplyToCommand(context, $"Finished playing {trackQueue.Dequeue().TrackName}");
            }
            skipFlag = false;
            if (trackQueue.Any()) await PlayNext(context, trackQueue.Peek());
            else playStatus = false;
            repeatFlag = false;
            utils.PurgeFile(track.TrackURL);
            UpdateLatestActivity();
            return;
        }

        /// <summary>
        /// Plays the next track in the queue and handles repeat, skip, and replay flags.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        /// <param name="track">The track to play.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PlayNext(InteractionContext context, Track track)
        {
            playStatus = true;
            DiscordMessage message = await context.GetOriginalResponseAsync();
            await FollowUpOnSlashCommand(context, $"Playing {track.TrackName} - {track.TrackDuration} ♪");
            await PlayAudio(context, track.TrackURL);

            if (replayFlag)
            {
                replayFlag = false;
                UpdateLatestActivity();
                await PlayNext(context, trackQueue.Peek());
                return;
            }
            if (skipFlag)
            {
                utils.PurgeFile(track.TrackURL);
                await FollowUpOnSlashCommand(context, $"Skipped {trackQueue.Dequeue().TrackName}");
            }
            else if (!repeatFlag)
            {
                utils.PurgeFile(track.TrackURL);
                await FollowUpOnSlashCommand(context, $"Finished playing {trackQueue.Dequeue().TrackName}");
            }
            skipFlag = false;
            if (trackQueue.Any()) await PlayNext(context, trackQueue.Peek());
            else playStatus = false;
            repeatFlag = false;
            utils.PurgeFile(track.TrackURL);
            UpdateLatestActivity();
            return;
        }

        /// <summary>
        /// Plays audio in a Discord voice channel by converting and streaming the audio file.
        /// </summary>
        /// <param name="context">The Discord command context or interaction context.</param>
        /// <param name="mediaPath">The path to the audio file to be played.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task PlayAudio(CommandContext context, string mediaPath)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            var transmit = botConnection.GetTransmitSink();
            if (!utils.CheckFile(mediaPath)) ConvertWEBMtoACC(mediaPath);
            var pcm = ConvertAudioToPcm(mediaPath);
            await pcm.CopyToAsync(transmit);
            await pcm.DisposeAsync();
            return;
        }
        /// <summary>
        /// Plays audio in a Discord voice channel by converting and streaming the audio file.
        /// </summary>
        /// <param name="context">The Discord interaction context or interaction context.</param>
        /// <param name="mediaPath">The path to the audio file to be played.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task PlayAudio(InteractionContext context, string mediaPath)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            var transmit = botConnection.GetTransmitSink();
            if (!utils.CheckFile(mediaPath)) ConvertWEBMtoACC(mediaPath);
            var pcm = ConvertAudioToPcm(mediaPath);
            await pcm.CopyToAsync(transmit);
            await pcm.DisposeAsync();
            return;
        }

        private void StartInactivityTimer<Context>(Context context)
        {
            if (inactivityTimer != null)
            {
                inactivityTimer.Dispose();
            }

            inactivityTimer = new Timer(async _ =>
            {
                if (!playStatus && (DateTime.Now - latestActivity) > TimeSpan.FromMinutes(1))
                {

                    if (context is CommandContext commandContext)
                    {
                        VoiceNextConnection botConnection = GetBotConnection(commandContext);
                        DiscordChannel? botChannel = botConnection?.TargetChannel;
                        playStatus = false;
                        StopFFMPEG();
                        trackQueue.Clear();
                        utils.ClearMediaDirectory();
                        await ReplyToCommand(commandContext, $"Leaving {botChannel?.Name} channel due to inactivity");
                        botConnection?.Disconnect();
                        return;
                    }
                    if (context is InteractionContext interactionContext)
                    {
                        VoiceNextConnection botConnection = GetBotConnection(interactionContext);
                        DiscordChannel? botChannel = botConnection?.TargetChannel;
                        playStatus = false;
                        StopFFMPEG();
                        trackQueue.Clear();
                        utils.ClearMediaDirectory();
                        if (botChannel != null)
                        { await interactionContext.Channel.SendMessageAsync($"Leaving {botChannel?.Name} channel due to inactivity"); }
                        botConnection?.Disconnect();
                        return;
                    }

                }
                else
                {
                    inactivityTimer?.Change((DateTime.Now - latestActivity) > TimeSpan.FromMinutes(1) ? TimeSpan.Zero : TimeSpan.FromMinutes(1) - (DateTime.Now - latestActivity), TimeSpan.FromMilliseconds(-1));
                }
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(-1));
        }
        #endregion




        #region  Bot Commands

        public async Task JoinCommand(CommandContext context)
        {
            {
                try
                {
                    VoiceNextConnection botConnection = GetBotConnection(context);
                    DiscordChannel? botChannel = botConnection?.TargetChannel;
                    DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                    if (memberConnection == null)
                    {
                        await ReplyToCommand(context, memberChannelResponse);
                        return;
                    }

                    DiscordChannel? memberChannel = memberConnection.Channel;

                    if (botConnection == null)
                    {
                        await ReplyToCommand(context, $"Joining {memberChannel?.Name}");
                        await memberChannel.ConnectAsync();
                        StartInactivityTimer(context);
                        return;
                    }

                    if (botConnection != null && botChannel != null && memberChannel?.Id != botChannel.Id)
                    {
                        await ReplyToCommand(context, $"Moving to {memberChannel?.Name}");
                        botConnection.Disconnect();
                        await memberChannel.ConnectAsync();
                        return;
                    }

                    await ReplyToCommand(context, $"Already joined to {memberChannel?.Name} channel");
                    return;
                }
                catch
                {
                    await ReplyToCommand(context, "Could not join voice channel");
                }
            }
        }
        public async Task JoinCommand(InteractionContext context)
        {
            {
                try
                {
                    await InitiateSlashCommand(context);

                    VoiceNextConnection botConnection = GetBotConnection(context);
                    DiscordChannel? botChannel = botConnection?.TargetChannel;
                    DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                    if (memberConnection == null)
                    {
                        await ReplyToCommand(context, memberChannelResponse);
                        return;
                    }

                    DiscordChannel? memberChannel = memberConnection.Channel;

                    if (botConnection == null)
                    {
                        await EndSlashCommand(context, $"Joining {memberChannel?.Name}");
                        await memberChannel.ConnectAsync();
                        StartInactivityTimer(context);
                        return;
                    }

                    if (botConnection != null && botChannel != null && memberChannel?.Id != botChannel.Id)
                    {
                        await EndSlashCommand(context, $"Moving to {memberChannel?.Name}");
                        botConnection.Disconnect();
                        await memberChannel.ConnectAsync();
                        return;

                    }

                    await ReplyToCommand(context, $"Already joined to {memberChannel?.Name} channel");
                    return;
                }
                catch
                {
                    await ReplyToCommand(context, "Could not join voice channel");
                }
            }
        }

        public async Task LeaveCommand(CommandContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
                    return;
                }

                playStatus = false;
                StopFFMPEG();
                trackQueue.Clear();
                utils.ClearMediaDirectory();
                await ReplyToCommand(context, $"Leaving {botChannel.Name} channel");
                botConnection?.Disconnect();
                return;
            }
            catch
            {
                await ReplyToCommand(context, "Could not leave voice channel");
            }
        }
        public async Task LeaveCommand(InteractionContext context)
        {
            try
            {
                await InitiateSlashCommand(context);

                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
                    return;
                }

                playStatus = false;
                trackQueue.Clear();
                utils.ClearMediaDirectory();
                await EndSlashCommand(context, $"Leaving {botChannel.Name} channel");
                botConnection?.Disconnect();
                return;
            }
            catch
            {
                await ReplyToCommand(context, "Could not leave voice channel");
            }
        }


        public async Task AddCommand(CommandContext context, params string[] path)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            var youtube = new Youtube();
            var youtubeClient = new YoutubeClient();


            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            if (path.Length > 0)
            {
                string joinedPath = string.Join(" ", path);
                await AddTrack(context, youtube, youtubeClient, joinedPath);
                return;
            }
            else
            {
                await ReplyToCommand(context, "Please add track name or URL to play");

            }
        }

        public async Task AddCommand(InteractionContext context, params string[] path)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            var youtube = new Youtube();
            var youtubeClient = new YoutubeClient();


            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            if (path.Length > 0)
            {
                string joinedPath = string.Join(" ", path);
                await AddTrack(context, youtube, youtubeClient, joinedPath);
                return;
            }
            else
            {
                await ReplyToCommand(context, "Please add track name or URL to play");
            }
        }

        public async Task PlayCommand(CommandContext context, params string[] path)
        {
            // try
            // {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }
            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }
            if (path.Length > 0)
            {
                if (!trackQueue.Any())
                {
                    await AddCommand(context, path);
                    await PlayNext(context, trackQueue.Peek());
                    return;
                }
                else
                {
                    await AddCommand(context, path);
                    return;
                }
            }
            else if (trackQueue.Any() && playStatus == false)
            {
                await PlayNext(context, trackQueue.Peek());
                return;
            }
            else
            {
                await ReplyToCommand(context, "Please add track name or URL to play");
                return;
            }

            // }
            // catch
            // {
            //     await ReplyToCommand(context, $"Unable to play track");
            // }
        }

        public async Task PlayCommand(InteractionContext context, params string[] path)
        {
            // try
            // {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }
            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }
            if (path.Length > 0)
            {
                await AddCommand(context, path);

                if (!trackQueue.Any())
                {
                    await AddCommand(context, path);
                    await PlayNext(context, trackQueue.Peek());
                    return;
                }
                else
                {
                    await AddCommand(context, path);
                    return;
                }
            }
            else if (trackQueue.Any() && playStatus == false)
            {
                await PlayNext(context, trackQueue.Peek());
                return;
            }
            else
            {
                await ReplyToCommand(context, "Please add track name or URL to play");
                return;
            }

            // }
            // catch
            // {
            //     await ReplyToCommand(context, $"Unable to play track");
            // }

        }

        public async Task PauseCommand(CommandContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            string PID = GetPID();
            PauseFFMPEG(ffmpegProcess);
            playStatus = false;
            await ReplyToCommand(context, $"Paused {trackQueue.Peek().TrackName}");
            return;
        }

        public async Task PauseCommand(InteractionContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            string PID = GetPID();
            PauseFFMPEG(ffmpegProcess);
            await ReplyToCommand(context, $"Paused {trackQueue.Peek().TrackName}");
            return;
        }
        public async Task ResumeCommand(CommandContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            string PID = GetPID();
            ResumeFFMPEG(ffmpegProcess);
            playStatus = true;
            await ReplyToCommand(context, $"Resumed {trackQueue.Peek().TrackName}");
            return;
        }

        public async Task ResumeCommand(InteractionContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            string PID = GetPID();
            ResumeFFMPEG(ffmpegProcess);
            await ReplyToCommand(context, $"Resumed {trackQueue.Peek().TrackName}");
            return;
        }

        public async Task StopCommand(CommandContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
                    return;
                }

                playStatus = false;
                StopFFMPEG();
                trackQueue.Clear();
                utils.ClearMediaDirectory();
                await ReplyToCommand(context, "Bot stopped playing and cleared Queue");
                return;
            }
            catch
            {
                await ReplyToCommand(context, "Could not stop player");

            }
        }
        public async Task StopCommand(InteractionContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
                    return;
                }

                playStatus = false;
                StopFFMPEG();
                trackQueue.Clear();
                utils.ClearMediaDirectory();
                await ReplyToCommand(context, "Bot stopped playing and cleared Queue");
                return;
            }
            catch
            {
                await ReplyToCommand(context, "Could not stop player");

            }
        }

        public async Task QueueCommand(CommandContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
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
                    await ReplyToCommand(context, message);
                    return;
                }
                else
                {
                    await ReplyToCommand(context, "Track queue is empty");
                    return;
                }

            }
            catch
            {
                await ReplyToCommand(context, "Could not show track queue");

            }

        }

        public async Task QueueCommand(InteractionContext context)

        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
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
                    await ReplyToCommand(context, message);
                    return;
                }
                else
                {
                    await ReplyToCommand(context, "Track queue is empty");
                    return;
                }

            }
            catch
            {
                await ReplyToCommand(context, "Could not show track queue");

            }

        }
        public async Task SkipCommand(CommandContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
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
                    await ReplyToCommand(context, "Track queue is empty");
                    return;
                }
            }
            catch
            {
                await ReplyToCommand(context, "Could not skip track");

            }
        }
        public async Task SkipCommand(InteractionContext context)
        {
            try
            {
                VoiceNextConnection botConnection = GetBotConnection(context);
                DiscordChannel? botChannel = botConnection?.TargetChannel;
                DiscordVoiceState? memberConnection = context.Member?.VoiceState;

                if (memberConnection == null)
                {
                    await ReplyToCommand(context, memberChannelResponse);
                    return;
                }

                if (botChannel == null)
                {
                    await ReplyToCommand(context, botChannelResponse);
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
                    await ReplyToCommand(context, "Track queue is empty");
                    return;
                }
            }
            catch
            {
                await ReplyToCommand(context, "Could not skip track");

            }
        }

        public async Task RepeatCommand(CommandContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }


            if (trackQueue.Any())
            {
                Track track = trackQueue.Peek();
                if (!repeatFlag)
                {
                    repeatFlag = true;
                    await ReplyToCommand(context, $"Repeating {track.TrackName} - {track.TrackDuration}");
                    return;
                }
                else
                {
                    repeatFlag = false;
                    await ReplyToCommand(context, $"Stopped repeating {track.TrackName} - {track.TrackDuration}");
                    return;
                }
            }
            else
            {
                await ReplyToCommand(context, "Track queue is empty");
                return;
            }
        }

        public async Task RepeatCommand(InteractionContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }


            if (trackQueue.Any())
            {
                Track track = trackQueue.Peek();
                if (!repeatFlag)
                {
                    repeatFlag = true;
                    await ReplyToCommand(context, $"Repeating {track.TrackName} - {track.TrackDuration}");
                    return;
                }
                else
                {
                    repeatFlag = false;
                    await ReplyToCommand(context, $"Stopped repeating {track.TrackName} - {track.TrackDuration}");
                    return;
                }
            }
            else
            {
                await ReplyToCommand(context, "Track queue is empty");
                return;
            }
        }

        public async Task ReplayCommand(CommandContext context)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            if (trackQueue.Any())
            {
                Track track = trackQueue.Peek();
                if (!replayFlag)
                {
                    replayFlag = true;
                    StopFFMPEG();
                    await ReplyToCommand(context, $"Replaying {track.TrackName} - {track.TrackDuration}");
                    return;
                }
                else
                {
                    replayFlag = false;
                    await ReplyToCommand(context, $"Stopped replaying {track.TrackName} - {track.TrackDuration}");
                    return;
                }
            }
            else
            {
                await ReplyToCommand(context, "Track queue is empty");
                return;
            }

        }
        public async Task ReplayCommand(InteractionContext context)
        {
            await InitiateSlashCommand(context);
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }

            if (trackQueue.Any())
            {
                Track track = trackQueue.Peek();
                if (!replayFlag)
                {
                    replayFlag = true;
                    StopFFMPEG();
                    await ReplyToCommand(context, $"Replaying {track.TrackName} - {track.TrackDuration}");
                    return;
                }
                else
                {
                    replayFlag = false;
                    await ReplyToCommand(context, $"Stopped replaying {track.TrackName} - {track.TrackDuration}");
                    return;
                }
            }
            else
            {
                await ReplyToCommand(context, "Track queue is empty");
                return;
            }
        }


        public async Task VolumeCommand(CommandContext context, long volume = 100)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }
            if (volume < 0 || volume > 100)
            {
                await ReplyToCommand(context, "Volume needs to be between 0 and 100% inclusive");
                return;
            }

            if (botConnection != null)
            {
                VoiceTransmitSink transmitStream = botConnection.GetTransmitSink();
                transmitStream.VolumeModifier = (double)volume / 100;
                await ReplyToCommand(context, $"Volume set to {volume}%");
                return;
            }
        }
        public async Task VolumeCommand(InteractionContext context, long volume = 100)
        {
            VoiceNextConnection botConnection = GetBotConnection(context);
            DiscordChannel? botChannel = botConnection?.TargetChannel;
            DiscordVoiceState? memberConnection = context.Member?.VoiceState;

            if (memberConnection == null)
            {
                await ReplyToCommand(context, memberChannelResponse);
                return;
            }

            if (botChannel == null)
            {
                await ReplyToCommand(context, botChannelResponse);
                return;
            }
            if (volume < 0 || volume > 100)
            {
                await ReplyToCommand(context, "Volume needs to be between 0 and 100% inclusive");
                return;
            }

            if (botConnection != null)
            {
                VoiceTransmitSink transmitStream = botConnection.GetTransmitSink();
                transmitStream.VolumeModifier = (double)volume / 100;
                await ReplyToCommand(context, $"Volume set to {volume}%");
                return;
            }
        }
        #endregion
    }
}
