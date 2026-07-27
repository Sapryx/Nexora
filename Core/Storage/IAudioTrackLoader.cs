using Core.Playback;

namespace Core.Storage;

public interface IAudioTrackLoader
{
    public Task<List<IAudioTrack>> Load();
}
