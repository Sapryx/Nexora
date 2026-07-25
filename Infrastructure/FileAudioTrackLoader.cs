using Core;

namespace Infrastructure;

public class FileAudioTrackLoader : IAudioTrackLoader
{
    private static readonly string[] SupportedExtensions = [
        ".mp3",
        ".flac",
        ".wav",
        ".opus",
        ".ogg"
    ];

    private readonly IMetadataLoader metadataLoader;

    public FileAudioTrackLoader(IMetadataLoader metadataLoader)
    {
        this.metadataLoader = metadataLoader;
    }

    public List<IAudioTrack> Load()
    {
        var musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        var musicFiles = Directory.GetFiles(musicDirectory);
        var audioTracks = new List<IAudioTrack>();

        foreach(string file in musicFiles)
        {
            string extension = Path.GetExtension(file);
            bool extensionIsSupported = SupportedExtensions.Contains(extension);

            if(!extensionIsSupported)
            {
                continue;
            }

            var metadata = metadataLoader.LoadMetadata(file);

            var audioTrack = new AudioTrack()
            {
                AudioPath = file,
                Metadata = metadata
            };
            
            audioTracks.Add(audioTrack);
        }

        return audioTracks;
    }
}
