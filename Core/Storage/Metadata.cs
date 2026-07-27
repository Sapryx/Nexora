namespace Core.Storage;

public class Metadata
{
    public string Title { get; set; } = "";
    public string Artists { get; set; } = "";
    public TimeSpan Duration { get; set; } = TimeSpan.Zero; // TODO Create a Properties object and move it there. It's not metadata.
    public byte[]? TrackCoverRaw { get; set; }
}
