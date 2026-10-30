using Core.Playback.Loudness;

namespace Core.Storage.Tags;

public interface IMetadataSaver
{
    public void Save(MetadataChange change);
    public void SaveLoudness(string audioPath, TrackLoudness loudness);
}
