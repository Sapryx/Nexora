namespace Core.Playback;

public class TrackProperties
{
    public TimeSpan Duration { get; }

    public TrackProperties(TimeSpan duration)
    {
        Duration = duration;
    }
}
