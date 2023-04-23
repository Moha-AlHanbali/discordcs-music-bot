
namespace MusicBot
{
    using YoutubeExplode;
    using YoutubeExplode.Common;
    using YoutubeExplode.Search;
    using YoutubeExplode.Videos;
    using YoutubeExplode.Videos.Streams;
    using YoutubeExplode.Playlists;


    class Youtube
    {
        Utils utils = new Utils();
        public async Task<VideoSearchResult> YoutubeSearch(YoutubeClient yt, string trackTitle)
        {
            var videos = await yt.Search.GetVideosAsync(trackTitle).CollectAsync(1);
            return videos[0];

            // For Future Enhancement:
            // foreach (VideoSearchResult video in videos)
            // {
            //     var id = video.Id;
            //     var title = video.Title;
            //     var url = video.Url;
            //     var duration = video.Duration;
            // }
        }

        public async Task<Video> YoutubeGrab(YoutubeClient yt, string trackURL)
        {
            var video = await yt.Videos.GetAsync(trackURL);
            return video;
        }

        public async Task<List<PlaylistVideo>> YoutubeGrabList(YoutubeClient yt, string trackURL)
        {
            List<PlaylistVideo> videoList = new List<PlaylistVideo>();
            await foreach (var video in yt.Playlists.GetVideosAsync(trackURL))
            {
                videoList.Add(video);
            }
            return videoList;
        }

        public async Task<string> YoutubeStream(YoutubeClient yt, string trackURL)
        {
            var streamManifest = await yt.Videos.Streams.GetManifestAsync(trackURL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var streamURL = streamInfo.Url;
            return streamURL;
        }

        public async Task<string> YoutubeDownload(YoutubeClient yt, string trackURL, Track track)
        {
            var streamManifest = await yt.Videos.Streams.GetManifestAsync(trackURL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            utils.CreateMediaDirectory();
            await yt.Videos.Streams.DownloadAsync(streamInfo, $"MediaTemp/{track.TrackName}.{streamInfo.Container}");
            return $"MediaTemp/{track.TrackName}.{streamInfo.Container}";
        }
    }
}