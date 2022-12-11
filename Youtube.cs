
namespace MusicBot
{
    using YoutubeExplode;
    using YoutubeExplode.Common;
    using YoutubeExplode.Search;
    class Youtube
    {
        public async Task YoutubeSearch(string songTitle)
        {
            var yt = new YoutubeClient();
            var videos = await yt.Search.GetVideosAsync(songTitle);
            foreach (VideoSearchResult video in videos.Take(5))
            {
                var id = video.Id;
                var title = video.Title;
                var url = video.Url;
                var duration = video.Duration;
            }
        }
    }
}