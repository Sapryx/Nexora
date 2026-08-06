using System.Collections.Concurrent;
using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class FileTrackLoader : ITrackLoader
{
    private readonly ILogger<FileTrackLoader> logger;
    private readonly IMetadataLoader metadataLoader;
    private readonly ITrackPropertyLoader propertyLoader;
    private readonly ISupportedAudioFormatsProvider supportedAudioFormatsProvider;
    private readonly IDegreeOfParallelismProvider degreeOfParallelismProvider;

    public FileTrackLoader(
        ILogger<FileTrackLoader> logger,
        IMetadataLoader metadataLoader,
        ITrackPropertyLoader propertyLoader,
        ISupportedAudioFormatsProvider supportedAudioFormatsProvider,
        IDegreeOfParallelismProvider<FileTrackLoader> degreeOfParallelismProvider)
    {
        this.logger = logger;
        this.metadataLoader = metadataLoader;
        this.propertyLoader = propertyLoader;
        this.supportedAudioFormatsProvider = supportedAudioFormatsProvider;
        this.degreeOfParallelismProvider = degreeOfParallelismProvider;
    }

    public List<IAudioTrack> Load()
    {
        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        var audioTracks = new ConcurrentBag<IAudioTrack>();
        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = degreeOfParallelismProvider.Value };
        var supportedFormats = supportedAudioFormatsProvider.GetFormats();
        
        var musicDirectoryEnumerator = Directory
            .EnumerateFiles(musicDirectory)
            .Where(file => supportedFormats.Contains(Path.GetExtension(file)));
        
        logger.Info($"Started loading tracks...");

        Parallel.ForEach(musicDirectoryEnumerator, parallelOptions, file =>
        {
            var metadata = metadataLoader.Load(file);
            var properties = propertyLoader.Load(file);
            var audioTrack = new AudioTrack(file, metadata, properties);
            
            audioTracks.Add(audioTrack);
            logger.Info($"Loaded track {audioTrack.ToString()}");
        });
        
        logger.Info($"Loaded {audioTracks.Count} tracks");

        return audioTracks.ToList();
    }
}
