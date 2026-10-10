using System.Collections.Concurrent;
using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class FileTrackLoader : ITrackLoader
{
    private readonly ILogger<FileTrackLoader> logger;
    private readonly IMetadataLoader metadataLoader;
    private readonly ISupportedAudioFormatsProvider supportedAudioFormatsProvider;
    private readonly IDegreeOfParallelismProvider degreeOfParallelismProvider;
    private readonly IMusicDirectoryProvider musicDirectoryProvider;

    public FileTrackLoader(
        ILogger<FileTrackLoader> logger,
        IMetadataLoader metadataLoader,
        ISupportedAudioFormatsProvider supportedAudioFormatsProvider,
        IDegreeOfParallelismProvider<FileTrackLoader> degreeOfParallelismProvider,
        IMusicDirectoryProvider musicDirectoryProvider)
    {
        this.logger = logger;
        this.metadataLoader = metadataLoader;
        this.supportedAudioFormatsProvider = supportedAudioFormatsProvider;
        this.degreeOfParallelismProvider = degreeOfParallelismProvider;
        this.musicDirectoryProvider = musicDirectoryProvider;
    }

    public List<IAudioTrack> Load()
    {
        var audioTracks = new ConcurrentBag<IAudioTrack>();
        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = degreeOfParallelismProvider.Value };
        var supportedFormats = supportedAudioFormatsProvider.GetFormats();

        var musicDirectoryEnumerator = musicDirectoryProvider
            .GetFiles()
            .Where(file => supportedFormats.Contains(Path.GetExtension(file)));

        logger.Info($"Started loading tracks...");

        Parallel.ForEach(musicDirectoryEnumerator, parallelOptions, file =>
        {
            try
            {
                var metadata = metadataLoader.Load(file);
                var audioTrack = new AudioTrack(file, metadata);

                audioTracks.Add(audioTrack);
                logger.Debug($"Loaded track {audioTrack.ToString()}");
            }
            catch(Exception ex)
            {
                logger.Warn(ex, $"Skipped unreadable file {file}");
            }
        });

        logger.Info($"Loaded {audioTracks.Count} tracks");

        return audioTracks
            .OrderBy(track => track.Metadata.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(track => track.Metadata.Artists, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
