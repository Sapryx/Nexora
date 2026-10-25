namespace Core.Playback;

public class Metadata
{
    public string Title { get; set; } = "";
    public IReadOnlyList<string> Artists { get; set; } = [];
    public TimeSpan Duration { get; set; }
    public string JoinedArtists => string.Join(", ", Artists);
}
