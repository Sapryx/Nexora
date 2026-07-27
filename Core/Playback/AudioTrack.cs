using Core.Storage;

namespace Core.Playback;

public class AudioTrack : IAudioTrack
{
    public string AudioPath { get; }
    public Metadata Metadata { get; }
    public TrackProperties Properties { get; }

    public AudioTrack(string audioPath, Metadata metadata, TrackProperties properties)
    {
        AudioPath = audioPath;
        Metadata = metadata;
        Properties = properties;
    }

    public override string ToString()
    {
        if(string.IsNullOrEmpty(Metadata.Artists))
        {
            return $"{Metadata.Title}";
        }
        else
        {
            return $"{Metadata.Artists} - {Metadata.Title}";
        }
    }
}
