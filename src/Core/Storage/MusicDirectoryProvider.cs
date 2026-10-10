using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class MusicDirectoryProvider : IMusicDirectoryProvider
{
    private readonly ILogger<MusicDirectoryProvider> logger;
    private readonly string musicDirectory;

    public MusicDirectoryProvider(ILogger<MusicDirectoryProvider> logger) 
        : this(
            logger, 
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyMusic,
                Environment.SpecialFolderOption.DoNotVerify
            )
        )
    {
    }

    public MusicDirectoryProvider(ILogger<MusicDirectoryProvider> logger, string musicDirectory)
    {
        this.logger = logger;
        this.musicDirectory = musicDirectory;
    }

    public IEnumerable<string> GetFiles()
    {
        if(!Directory.Exists(musicDirectory))
        {
            logger.Warn($"Music directory '{musicDirectory}' does not exist");
            return [];
        }

        logger.Info($"Loading tracks from {musicDirectory}");
        return Directory.EnumerateFiles(musicDirectory);
    }
}
