using Avalonia.Media.Imaging;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Media;

public class ThumbnailStore : IThumbnailStore
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

    private static bool IsFresh(FileInfo file, string audioPath)
    {
        return file.Exists && file.LastWriteTimeUtc >= File.GetLastWriteTimeUtc(audioPath);
    }
}
