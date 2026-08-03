using Core.Playlists;

namespace Core.Playback;

public class AudioPlayer : IAudioPlayer
{
    public IPlaylistItem? NowPlaying { get; private set; }
    public bool IsPlaying => audioEngine.IsPlaying;

    public int Volume
    {
        get => audioEngine.Volume;
        set => audioEngine.Volume = value;
    }

    public float PlaybackPosition
    {
        get => audioEngine.PlaybackPosition;
        set => audioEngine.PlaybackPosition = value;
    }

    private readonly IAudioEngine audioEngine;

    public event Action? PlaybackStarted
    {
        add => audioEngine.PlaybackStarted += value;
        remove => audioEngine.PlaybackStarted -= value;
    }

    public event Action<float>? PlaybackPositionChanged
    {
        add => audioEngine.PlaybackPositionChanged += value;
        remove => audioEngine.PlaybackPositionChanged -= value;
    }

    public event Action? PlaybackPaused
    {
        add => audioEngine.PlaybackPaused += value;
        remove => audioEngine.PlaybackPaused -= value;
    }
    
    public event Action? PlaybackFinished
    {
        add => audioEngine.PlaybackFinished += value;
        remove => audioEngine.PlaybackFinished -= value;
    }
    
    public event Action? PlaybackResumed
    {
        add => audioEngine.PlaybackResumed += value;
        remove => audioEngine.PlaybackResumed -= value;
    }
    
    public event Action<float>? VolumeChanged
    {
        add => audioEngine.VolumeChanged += value;
        remove => audioEngine.VolumeChanged -= value;
    }

    public AudioPlayer(IAudioEngine audioEngine)
    {
        this.audioEngine = audioEngine;
    }

    public void PlayTrack(IPlaylistItem playlistItem)
    {
        NowPlaying = playlistItem;
        audioEngine.StartPlayback(NowPlaying.AudioTrack);
    }

    public void TogglePause()
    {
        if(NowPlaying == null)
        {
            return;
        }
        
        audioEngine.TogglePause();
    }
}
