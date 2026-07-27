namespace Core.Playback;

public interface IAudioEngine
{
    public bool IsPlaying { get; }
    public int Volume { get; set; }
    public float PlaybackPosition { get; set; }

    public event Action<float>? PlaybackPositionChanged;
    public event Action? PlaybackStarted;
    public event Action? PlaybackFinished;
    public event Action? PlaybackPaused;
    public event Action? PlaybackResumed;
    
    public void StartPlayback(IAudioTrack audioTrack);
    public void TogglePause();
}
