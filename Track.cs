
namespace MusicBot
{
    public class Track
    {
        public string TrackName { get; set; }
        public string TrackURL { get; set; }
        public TimeSpan? TrackDuration { get; set; }

        public Track(string trackName, string trackURL, TimeSpan? trackDuration)
        {
            TrackName = trackName;
            TrackURL = trackURL;
            TrackDuration = trackDuration;
        }

    }
}