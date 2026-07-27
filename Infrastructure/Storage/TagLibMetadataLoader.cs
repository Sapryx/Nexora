using System.Diagnostics.CodeAnalysis;
using Core.Storage;
using TagLib;
using File = TagLib.File;

namespace Infrastructure.Storage;

public class TagLibMetadataLoader : IMetadataLoader
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Mpeg.AudioFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Riff.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Ogg.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Flac.File))]
    public Metadata LoadMetadata(string filePath)
    {
        using var tagFile = File.Create(filePath);
        string title = tagFile.Tag.Title;
        var artists = string.Join(", ", tagFile.Tag.Performers);
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
            TrackCoverRaw = albumCoverRaw
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
