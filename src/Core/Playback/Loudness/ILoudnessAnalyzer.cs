using Core.Library;

namespace Core.Playback.Loudness;

public interface ILoudnessAnalyzer
{
    public TrackLoudness? Analyze(string audioPath);
}
