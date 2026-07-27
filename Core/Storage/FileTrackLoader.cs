using System.Collections.Concurrent;
using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class FileTrackLoader : ITrackLoader
{
    private static readonly HashSet<string> SupportedExtensions = [
        ".mp3",
        ".flac",
        ".wav",
        ".opus",
        ".ogg"
    ];

    private readonly ILogger<FileTrackLoader> logger;
    private readonly IMetadataLoader metadataLoader;
    private readonly int degreeOfParallelism;

    public FileTrackLoader(
        ILogger<FileTrackLoader> logger,
        IMetadataLoader metadataLoader, 
        int degreeOfParallelism)
    {
        this.logger = logger;
        this.metadataLoader = metadataLoader;
        this.degreeOfParallelism = degreeOfParallelism;
    }

    public List<IAudioTrack> Load()
    {
        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        var audioTracks = new ConcurrentBag<IAudioTrack>();
        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = degreeOfParallelism };
        
        var musicDirectoryEnumerator = Directory
            .EnumerateFiles(musicDirectory)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file)));
        
        logger.Info($"Started loading tracks...");
        
        Parallel.ForEach(musicDirectoryEnumerator, parallelOptions, file =>
        {
            var metadata = metadataLoader.LoadMetadata(file);
            var audioTrack = new AudioTrack()
            {
                AudioPath = file,
                Metadata = metadata
            };
            
            audioTracks.Add(audioTrack);
            
            logger.Info($"Loaded track {audioTrack.Metadata.Artists} - {audioTrack.Metadata.Title}");
        });
        
        logger.Info($"Loaded {audioTracks.Count} tracks");

        return audioTracks.ToList();
    }
}
