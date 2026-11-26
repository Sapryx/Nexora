using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Media;

public partial class ThumbnailStore : IThumbnailStore
{
    private readonly string directory;
    private readonly ILogger<ThumbnailStore> logger;

    public ThumbnailStore(string directory, ILogger<ThumbnailStore> logger)
    {
        this.directory = directory;
        this.logger = logger;
    }

    public string PathOf(string audioPath, int pixelSide)
    {
        string name = Path.GetFileNameWithoutExtension(audioPath);
        return Path.Combine(directory, $"{name} {pixelSide}x{pixelSide}.png");
    }

    public bool IsFresh(string audioPath, int pixelSide)
    {
        return IsFresh(new FileInfo(PathOf(audioPath, pixelSide)), audioPath);
    }

    public bool TryLoad(string audioPath, int pixelSide, out Bitmap? thumbnail)
    {
        thumbnail = null;
        var file = new FileInfo(PathOf(audioPath, pixelSide));

        if(!IsFresh(file, audioPath))
        {
            return false;
        }

        try
        {
            thumbnail = file.Length == 0 ? null : new Bitmap(file.FullName);
            return true;
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to read thumbnail {file.FullName}");
            return false;
        }
    }

    public void Save(string audioPath, int pixelSide, Bitmap? thumbnail)
    {
        string path = PathOf(audioPath, pixelSide);
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            Directory.CreateDirectory(directory);

            using(var stream = File.Create(temporaryPath))
            {
                thumbnail?.Save(stream, PngBitmapEncoderOptions.Default);
            }

            File.Move(temporaryPath, path, true);
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to save thumbnail {path}");
        }
        finally
        {
            if(File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public void RemoveOrphanedThumbnails(IEnumerable<string> audioPaths, CancellationToken token)
    {
        var trackNames = audioPaths.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.Ordinal);

        try
        {
            foreach(string path in Directory.EnumerateFiles(directory, "*.png"))
            {
                token.ThrowIfCancellationRequested();

                if(IsOrphanedThumbnail(Path.GetFileName(path), trackNames))
                {
                    File.Delete(path);
                }
            }
        }
        catch(DirectoryNotFoundException)
        {
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        {
            logger.Warn(ex, $"Failed to remove orphaned thumbnails in {directory}");
        }
    }

    private static bool IsOrphanedThumbnail(string fileName, HashSet<string?> trackNames)
    {
        var name = ThumbnailName().Match(fileName);
        return name.Success && int.TryParse(name.Groups["size"].ValueSpan, out _)
            && !trackNames.Contains(name.Groups["track"].Value);
    }

    [GeneratedRegex(@"\A(?<track>.*) (?<size>[1-9][0-9]*)x\k<size>\.png\z", RegexOptions.Singleline)]
    private static partial Regex ThumbnailName();

    private static bool IsFresh(FileInfo file, string audioPath)
    {
        return file.Exists && file.LastWriteTimeUtc >= File.GetLastWriteTimeUtc(audioPath);
    }
}
