namespace Core.Playback;

public interface IMediaEngine
{
    public float Position { get; set; }
    public long Time { get; }
    public int Volume { get; set; }
    public bool Mute { get; set; }
    public bool HoldsFile { get; }

    public event Action<float>? PositionChanged;
    public event Action? EndReached;
    public event Action? EncounteredError;
    public event Action<float>? VolumeChanged;
    public event Action<bool>? MuteChanged;

    public void Open(string audioPath, long startTimeMs, bool startPaused, double gainDb);
    public void SetPause(bool isPaused);
    public void Stop();
}
