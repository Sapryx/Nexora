using Core.Library;

namespace Core.Playback.Loudness;

public static class LoudnessGain
{
    public const double TargetLufs = -14;
    private const double PeakCeilingDb = -1;
    private const double MaxGainDb = 20;

    public static double Calculate(TrackLoudness loudness, double targetLufs)
    {
        double gain = targetLufs - loudness.Lufs;

        if(gain > 0)
        {
            double headroom = loudness.PeakDb != null ? PeakCeilingDb - loudness.PeakDb.Value : 0;
            gain = Math.Min(gain, Math.Max(headroom, 0));
        }

        return Math.Clamp(gain, -MaxGainDb, MaxGainDb);
    }
}
