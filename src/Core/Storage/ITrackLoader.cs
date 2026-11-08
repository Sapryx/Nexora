using Core.Library;

namespace Core.Storage;

public interface ITrackLoader
{
    public List<IAudioTrack> Load();
}
