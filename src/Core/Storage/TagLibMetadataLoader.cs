using System.Diagnostics.CodeAnalysis;
using Core.Playback;
using File = TagLib.File;

namespace Core.Storage;

public class TagLibMetadataLoader : IMetadataLoader
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Mpeg.AudioFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Riff.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Ogg.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Flac.File))]
    public Metadata Load(string filePath)
    {
        using var tagFile = File.Create(filePath);
        string title = tagFile.Tag.Title;
        string artists = string.Join(", ", tagFile.Tag.Performers);

        if(string.IsNullOrEmpty(title))
        {
            title = Path.GetFileNameWithoutExtension(tagFile.Name);
        }

        return new Metadata()
        {
            Title = title,
            Artists = artists,
            Duration = tagFile.Properties.Duration
        };
    }
}
