using Core.Playback;

namespace Core.Library;

public class Artist
{
    public string Name { get; }
    public IReadOnlyList<IAudioTrack> Tracks { get; }

    public Artist(string name, IReadOnlyList<IAudioTrack> tracks)
    {
        Name = name;
        Tracks = tracks;
    }
}
