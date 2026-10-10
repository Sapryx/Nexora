using System.Globalization;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class FileVolumeStorage : IVolumeStorage
{
    private const int MinVolume = 0;
    private const int MaxVolume = 100;
    private readonly string filePath;
    private readonly ILogger logger;
    private bool isSaveFailureReported;

    public FileVolumeStorage(string filePath, ILogger<FileVolumeStorage> logger)
    {
        this.filePath = filePath;
        this.logger = logger;
    }

    public int? Load()
    {
        if(!File.Exists(filePath))
        {
            logger.Info($"No saved volume at {filePath}");
            return null;
        }

        try
        {
            string text = File.ReadAllText(filePath).Trim();

            if(!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int volume))
            {
                logger.Warn($"Ignored saved volume '{text}' in {filePath}: not a number");
                return null;
            }

            volume = Math.Clamp(volume, MinVolume, MaxVolume);
            logger.Info($"Restored volume {volume} from {filePath}");

            return volume;
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to read saved volume from {filePath}");
            return null;
        }
    }

    public void Save(int volume)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);

            if(directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, volume.ToString(CultureInfo.InvariantCulture));
            isSaveFailureReported = false;
        }
        catch(Exception ex)
        {
            if(!isSaveFailureReported)
            {
                logger.Warn(ex, $"Failed to save volume to {filePath}");
                isSaveFailureReported = true;
            }
        }
    }
}
