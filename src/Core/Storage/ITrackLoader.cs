using Core.Playback;

namespace Core.Storage;

public interface ITrackLoader
{
    public List<IAudioTrack> Load();
}
