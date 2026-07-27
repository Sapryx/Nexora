using Core.Playback;

namespace Core.Storage;

public interface IAudioTrackLoader
{
    public List<IAudioTrack> Load();
}
