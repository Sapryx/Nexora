namespace Core;

public class AudioTrack : IAudioTrack
{
    public string AudioPath { get; set; } = "";
    public Metadata Metadata { get; set; } = new Metadata();
}
