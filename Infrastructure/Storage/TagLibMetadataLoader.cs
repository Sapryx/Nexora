using Core.Storage;
using TagLib;
using File = TagLib.File;

namespace Infrastructure.Storage;

public class TagLibMetadataLoader : IMetadataLoader
{
    public Metadata LoadMetadata(string filePath)
    {
        using var tagFile = File.Create(filePath);
        string title = tagFile.Tag.Title;
        var artists = tagFile.Tag.Performers;
        var duration = tagFile.Properties.Duration;
        var albumCoverRaw = LoadAlbumCover(tagFile.Tag);

        if(string.IsNullOrEmpty(title))
        {
            title = Path.GetFileNameWithoutExtension(tagFile.Name);
        }

        return new Metadata()
        {
            Title = title,
            Artists = artists,
            Duration = duration,
            AlbumCoverRaw = albumCoverRaw
        };
    }

    private byte[]? LoadAlbumCover(Tag tag)
    {
        if(tag.Pictures.Length > 0)
        {
            return tag.Pictures[0].Data.Data;
        }
        else
        {
            return null;
        }
    }
}
