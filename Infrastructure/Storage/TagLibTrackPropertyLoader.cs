using Core.Playback;
using Core.Storage;
using File = TagLib.File;

namespace Infrastructure.Storage;

public class TagLibTrackPropertyLoader : ITrackPropertyLoader
{
    public TrackProperties Load(string filePath)
    {
        using var tagFile = File.Create(filePath);
        var duration = tagFile.Properties.Duration;

        return new TrackProperties(duration);
    }
}
