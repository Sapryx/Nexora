namespace Core.Storage;

public class Metadata
{
    public string Title { get; set; } = "";
    public string Artists { get; set; } = "";
    public byte[]? TrackCoverRaw { get; set; }
}
