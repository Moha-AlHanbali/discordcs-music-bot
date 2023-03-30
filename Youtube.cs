
namespace MusicBot
{
    using YoutubeExplode;
    using YoutubeExplode.Common;
    using YoutubeExplode.Search;
    using YoutubeExplode.Videos;
    using YoutubeExplode.Videos.Streams;

    class Youtube
    {
        Utils utils = new Utils();
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

        public async Task<Video> YoutubeGrab(YoutubeClient yt, string songURL)
        {
            var video = await yt.Videos.GetAsync(songURL);
            return video;
        }

        public async Task<string> YoutubeStream(YoutubeClient yt, string songURL)
        {
            var streamManifest = await yt.Videos.Streams.GetManifestAsync(songURL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var streamURL = streamInfo.Url;
            return streamURL;
        }

        public async Task<string> YoutubeDownload(YoutubeClient yt, string songURL, Track track)
        {
            var streamManifest = await yt.Videos.Streams.GetManifestAsync(songURL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            utils.ClearMediaDirectory();
            await yt.Videos.Streams.DownloadAsync(streamInfo, $"MediaTemp/{track.TrackName}.{streamInfo.Container}");
            return $"MediaTemp/{track.TrackName}.{streamInfo.Container}";
        }
    }
}