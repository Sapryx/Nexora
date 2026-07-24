namespace Core;

public class Metadata
{
    public string Title { get; set; } = "";
    public string[] Artists { get; set; } = [];
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public byte[]? AlbumCoverRaw { get; set; }
}
