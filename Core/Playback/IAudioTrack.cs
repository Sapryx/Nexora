using Core.Storage;

namespace Core.Playback;

public interface IAudioTrack
{
    public string AudioPath { get; }
    public Metadata Metadata { get; }
    public TrackProperties Properties { get; }
}
