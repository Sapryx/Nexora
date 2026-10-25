using System.Diagnostics.CodeAnalysis;
using File = TagLib.File;

namespace Core.Storage;

public class TagLibMetadataSaver : IMetadataSaver
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Mpeg.AudioFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Riff.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Ogg.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Flac.File))]
    public void Save(MetadataChange change)
    {
        using var tagFile = File.Create(change.AudioPath);

        if(change.Title != null)
        {
            tagFile.Tag.Title = change.Title == "" ? null : change.Title;
        }

        if(change.Artists != null)
        {
            tagFile.Tag.Performers = change.Artists.ToArray();
        }

        tagFile.Save();
    }
}
