using Core.Playback;

namespace Core.Storage;

public interface ITrackPropertyLoader
{
    public TrackProperties Load(string filePath);
}
