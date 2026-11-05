using Core.Playback;

namespace Core.Tests.Playback;

public class FakeMediaEngine : IMediaEngine
{
    public float Position { get; set; }
    public long Time { get; set; }
    public int Volume { get; set; }
    public bool Mute { get; set; }
    public bool HoldsFile { get; set; }
    public string? OpenedPath { get; private set; }
    public long OpenedStartTimeMs { get; private set; }
    public double OpenedGainDb { get; private set; }
    public int OpenCount { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsStopped { get; private set; }

    public event Action<float>? PositionChanged;
    public event Action? EndReached;
    public event Action? EncounteredError;
    public event Action<float>? VolumeChanged;
    public event Action<bool>? MuteChanged;

    public void Open(string audioPath, long startTimeMs, bool startPaused, double gainDb)
    {
        OpenedPath = audioPath;
        OpenedStartTimeMs = startTimeMs;
        OpenedGainDb = gainDb;
        OpenCount++;
        IsPaused = startPaused;
        IsStopped = false;
        HoldsFile = true;
    }

    public void SetPause(bool isPaused)
    {
        IsPaused = isPaused;
    }

    public void Stop()
    {
        IsStopped = true;
        HoldsFile = false;
    }

    public void RaisePositionChanged(float position)
    {
        PositionChanged?.Invoke(position);
    }

    public void RaiseEndReached()
    {
        HoldsFile = false;
        EndReached?.Invoke();
    }

    public void RaiseEncounteredError()
    {
        HoldsFile = false;
        EncounteredError?.Invoke();
    }

    public void RaiseVolumeChanged(float volume)
    {
        VolumeChanged?.Invoke(volume);
    }

    public void RaiseMuteChanged(bool isMuted)
    {
        MuteChanged?.Invoke(isMuted);
    }
}
