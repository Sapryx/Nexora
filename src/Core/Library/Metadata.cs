namespace Core.Library;

public class Metadata
{
    public string Title { get; set; } = "";
    public IReadOnlyList<string> Artists { get; set; } = [];
    public TimeSpan Duration { get; set; }
    public TrackLoudness? Loudness { get; set; }
    public string JoinedArtists => string.Join(", ", Artists);

    public static string GetDisplayedTitle(string? title, string audioPath)
    {
        return string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(audioPath) : title;
    }
}
