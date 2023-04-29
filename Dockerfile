# Stage 1 - Build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app
COPY . .
RUN apt-get update && apt-get install -y ffmpeg libopus0 libopus-dev
RUN dotnet restore
RUN dotnet publish -c Release -o release

# Stage 2 - Run the application
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app/release
COPY --from=build /app/release .
COPY .env .
EXPOSE 80
ENTRYPOINT ["dotnet", "discordcs-music-bot.dll"]
