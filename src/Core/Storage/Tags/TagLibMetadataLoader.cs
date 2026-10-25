using System.Diagnostics.CodeAnalysis;
using Core.Playback;
using File = TagLib.File;

namespace Core.Storage.Tags;

public class TagLibMetadataLoader : IMetadataLoader
{
    private const StringSplitOptions ArtistSplitOptions =
        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
    private static readonly char[] ArtistSeparators = [',', '&'];

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Mpeg.AudioFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Riff.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Ogg.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Flac.File))]
    public Metadata Load(string filePath)
    {
        using var tagFile = File.Create(filePath);
        string title = tagFile.Tag.Title;

        if(string.IsNullOrEmpty(title))
        {
            title = Path.GetFileNameWithoutExtension(tagFile.Name);
        }

        return new Metadata()
        {
            Title = title,
            Artists = SplitArtists(tagFile.Tag.Performers),
            Duration = tagFile.Properties.Duration
        };
    }

    public static IReadOnlyList<string> SplitArtists(IEnumerable<string> performers)
    {
        return performers
            .SelectMany(it => it.Split(ArtistSeparators, ArtistSplitOptions))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
