# Stage 1 - Build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

RUN apt-get update && apt-get install -y libopus0 libopus-dev libsodium23 libsodium-dev

WORKDIR /app
COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o release


# Stage 2 - Run the application
FROM mcr.microsoft.com/dotnet/runtime:6.0

RUN apt-get update && apt-get install -y ffmpeg

WORKDIR /app/release
COPY --from=build /app/release . 
COPY --from=build /usr/lib/x86_64-linux-gnu/libopus.* /app/release
COPY --from=build /usr/lib/x86_64-linux-gnu/libsodium.* /app/release

COPY .env .

EXPOSE 80

ENTRYPOINT ["dotnet", "discordcs-music-bot.dll"]
