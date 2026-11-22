using Core.Tracks;

namespace Core.Storage;

public interface ITrackLoader
{
    public List<IAudioTrack> Load();
}
