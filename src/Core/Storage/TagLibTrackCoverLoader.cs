using File = TagLib.File;

namespace Core.Storage;

public class TagLibTrackCoverLoader : ITrackCoverLoader
{
    public byte[]? LoadCover(string audioPath)
    {
        using var tagFile = File.Create(audioPath);
        var pictures = tagFile.Tag.Pictures;
        
        return pictures.Length > 0
            ? pictures[0].Data.Data
            : null;
    }
}
