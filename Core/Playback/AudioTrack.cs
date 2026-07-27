using Core.Storage;

namespace Core.Playback;

public class AudioTrack : IAudioTrack
{
    public string AudioPath { get; set; } = "";
    public Metadata Metadata { get; set; } = new Metadata();

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
