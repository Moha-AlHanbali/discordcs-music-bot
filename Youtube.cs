
namespace MusicBot
{
    using YoutubeExplode;
    using YoutubeExplode.Common;
    using YoutubeExplode.Search;
    using YoutubeExplode.Videos.Streams;

    class Youtube
    {
        public async Task<VideoSearchResult> YoutubeSearch(YoutubeClient yt, string songTitle)
        {
            var videos = await yt.Search.GetVideosAsync(songTitle);
            return videos[0];

            // For Future Enhancement
            // foreach (VideoSearchResult video in videos.Take(5))
            // {
            //     var id = video.Id;
            //     var title = video.Title;
            //     var url = video.Url;
            //     var duration = video.Duration;
            // }
        }

        public async Task<string> YoutubeStream(YoutubeClient yt, string songURL)
        {
            var streamManifest = await yt.Videos.Streams.GetManifestAsync(songURL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var streamURL = streamInfo.Url;
            // var stream = await yt.Videos.Streams.GetAsync(streamInfo);
            // await yt.Videos.Streams.DownloadAsync(streamInfo, $"video.{streamInfo.Container}");
            return streamURL;
        }

    }
}