using System.Collections.Concurrent;
using Core.Playback;

namespace Core.Storage;

public class FileTrackLoader : IAudioTrackLoader
{
    private static readonly string[] SupportedExtensions = [
        ".mp3",
        ".flac",
        ".wav",
        ".opus",
        ".ogg"
    ];

    private readonly IMetadataLoader metadataLoader;

    public FileTrackLoader(IMetadataLoader metadataLoader)
    {
        this.metadataLoader = metadataLoader;
    }

    public async Task<List<IAudioTrack>> Load()
    {
        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        var audioTracks = new ConcurrentBag<IAudioTrack>();
        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        
        var musicDirectoryEnumerator = Directory
            .EnumerateFiles(musicDirectory)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file)));
        
        Parallel.ForEach(musicDirectoryEnumerator, parallelOptions, file =>
        {
            var metadata = metadataLoader.LoadMetadata(file);
            
            audioTracks.Add(new AudioTrack()
            {
                AudioPath = file,
                Metadata = metadata
            });
        });

        return audioTracks.ToList();
    }
}
