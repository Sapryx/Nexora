using Core.Playlists;

namespace Core.Playback;

public interface IAudioPlayer
{
    public IPlaylistItem? NowPlaying { get; }
    public bool IsPlaying { get; }
    public int Volume { get; set; }
    public float PlaybackPosition { get; set; }
    
    public event Action? PlaybackStarted;
    public event Action<float>? PlaybackPositionChanged;
    public event Action? PlaybackPaused;
    public event Action? PlaybackFinished;
    public event Action? PlaybackResumed;
    
    public void PlayTrack(IPlaylistItem playlistItem);
    public void TogglePause();
}
