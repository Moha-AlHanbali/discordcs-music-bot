# discordcs-music-bot

## Getting Started

### Using bot locally

- Prerequisite:
  - Have [.Net 6.0 SDK and Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and [FFmpeg](https://ffmpeg.org/download.html) installed on your machine
  - Install required [Voice Natives](https://dsharpplus.github.io/DSharpPlus/articles/audio/voicenext/prerequisites.html) to connect to voice channels
- Navigate to where `discordcs-music-bot.csproj` is
- Create `.env` file and add your token as shown in `.env.sample` file
- Run `dotnet restore` command to restore dependencies
- Run the bot using `dotnet run` command

### Running bot in a container

- Prerequisite: Have `Docker` setup and running on your machine
- Navigate to where `discordcs-music-bot.csproj` is
- Build a Docker image using `docker build -t music-bot .` command
- Run the image in a container using `docker run -p8080:80  music-bot` command

## Functionality

- [x] Get it running
- [x] Add the ability to join/leave VC
- [x] Add the ability to play via urls
- [x] Add the ability to search for tracks
- [x] Add the ability to pause/resume
- [x] Add the ability to stop/skip
- [x] Add the ability to Replay
- [x] Add the ability to Repeat
- [x] Add Queue command
- [x] Detect player state accurately and respond accordingly
- [x] Accept Playlists
- [x] Download ~~/Stream~~ mode
- [X] Inactivity Timeout
- [x] Slash Commands
- [x] Volume Control
- [x] Containerize it
- [ ] Handle Errors (Somewhat)

## Improvements

- [x] Enhance Search time
- [ ] Enhance Download performance
- [ ] Detect generic playlists
- [ ] Add Track Search results
- [ ] Add Control Buttons
- [ ] Refactor Commands Core
