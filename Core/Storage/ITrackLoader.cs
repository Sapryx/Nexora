using Core.Playback;

namespace Core.Storage;

public interface ITrackLoader
{
    public Task<List<IAudioTrack>> Load();
}
