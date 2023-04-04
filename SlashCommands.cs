
namespace MusicBot
{
    using DSharpPlus.SlashCommands;
    using DSharpPlus.CommandsNext;
    using System.IO;
    using System.Diagnostics;
    using DSharpPlus.Entities;
    using DSharpPlus.VoiceNext;
    using DSharpPlus.CommandsNext.Attributes;
    using YoutubeExplode;
    using DSharpPlus;

    public class SlashCommands : ApplicationCommandModule
    {
        Utils utils;
        Queue<Track> trackQueue;
        Boolean playStatus;
        Boolean skipFlag;
        Boolean repeatFlag;
        Boolean replayFlag;
        String botChannelResponse;
        String memberChannelResponse;

        public SlashCommands(Utils utils, Queue<Track> trackQueue, MusicBot.Program.BotCommandsOptions options)
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

        private async Task ReplyToCommand(InteractionContext context, string message)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent((message)));
            return;
        }

        private VoiceNextConnection GetBotConnection(InteractionContext context)
        {
            var voiceNext = context.Client.GetVoiceNext();
            return voiceNext.GetConnection(context.Guild);
        }

        [SlashCommand("join", "A slash command that instructs the bot to join your voice channel.")]
        public async Task JoinCommand(InteractionContext context)
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
                    await ReplyToCommand(context, $"Joining {memberChannel?.Name} . . .");
                    await memberChannel.ConnectAsync();
                    return;
                }

                if (botConnection != null && botChannel != null && memberChannel?.Id != botChannel.Id)
                {
                    await ReplyToCommand(context, $"Moving to {memberChannel?.Name} . . .");
                    botConnection.Disconnect();
                    await memberChannel.ConnectAsync();
                    return;
                }
                await ReplyToCommand(context, $"Already joined to {memberChannel?.Name} channel");
                return;
            }
            catch
            {
                await ReplyToCommand(context, "Could not join channel..");
            }
        }
    }
}