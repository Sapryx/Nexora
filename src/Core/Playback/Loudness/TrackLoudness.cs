namespace Core.Playback.Loudness;

public class TrackLoudness
{
    public required double Lufs { get; init; }
    public double? PeakDb { get; init; }
}
