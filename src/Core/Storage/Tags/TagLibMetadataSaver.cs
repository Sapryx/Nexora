using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Core.Library;
using TagLib;
using File = TagLib.File;

namespace Core.Storage.Tags;

public class TagLibMetadataSaver : IMetadataSaver
{
    private const string TempFileSuffix = ".nexora-tmp";
    private readonly ConcurrentDictionary<string, Lock> fileLocks = new ConcurrentDictionary<string, Lock>();

    public void Save(MetadataChange change)
    {
        SaveAtomically(change.AudioPath, tagFile =>
        {
            if(change.Title != null)
            {
                tagFile.Tag.Title = change.Title == "" ? null : change.Title;
            }

            if(change.Artists != null)
            {
                tagFile.Tag.Performers = change.Artists.ToArray();
            }
        });
    }

    public void SaveLoudness(string audioPath, TrackLoudness loudness)
    {
        SaveAtomically(audioPath, tagFile => LoudnessTags.Write(tagFile, loudness));
    }

    public static string GetTempPath(string audioPath)
    {
        string directory = Path.GetDirectoryName(audioPath) ?? "";

        return Path.Combine(directory, $".{Path.GetFileName(audioPath)}{TempFileSuffix}");
    }

    private void SaveAtomically(string audioPath, Action<File> modify)
    {
        lock(fileLocks.GetOrAdd(audioPath, it => new Lock()))
        {
            ReplaceAtomically(audioPath, modify);
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Mpeg.AudioFile))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Riff.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Ogg.File))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TagLib.Flac.File))]
    private static void ReplaceAtomically(string audioPath, Action<File> modify)
    {
        string tempPath = GetTempPath(audioPath);
        string mimeType = $"taglib/{Path.GetExtension(audioPath).TrimStart('.').ToLowerInvariant()}";

        try
        {
            System.IO.File.Copy(audioPath, tempPath, true);

            using(var tagFile = File.Create(tempPath, mimeType, ReadStyle.Average))
            {
                modify(tagFile);
                tagFile.Save();
            }

            System.IO.File.Move(tempPath, audioPath, true);
        }
        finally
        {
            System.IO.File.Delete(tempPath);
        }
    }
}
