using Core;
using LibVLCSharp.Shared;

namespace Infrastructure;

public class VlcAudioEngine : IAudioEngine
{
    public bool IsPlaying => mediaPlayer.IsPlaying;

    public int Volume
    {
        get => mediaPlayer.Volume;
        set => mediaPlayer.Volume = value;
    }

    public float PlaybackPosition
    {
        get => mediaPlayer.Position;
        set => mediaPlayer.Position = value;
    }

    private readonly LibVLC vlc;
    private readonly MediaPlayer mediaPlayer;
    
    public event Action<float>? PlaybackPositionChanged;
    public event Action? PlaybackStarted;
    public event Action? PlaybackFinished;
    public event Action? PlaybackPaused;
    public event Action? PlaybackResumed;
    
    public VlcAudioEngine(LibVLC vlc)
    {
        this.vlc = vlc;
        mediaPlayer = new MediaPlayer(vlc);
        mediaPlayer.PositionChanged += OnPlaybackPositionChanged;
        mediaPlayer.Playing += OnPlaybackStarted;
        mediaPlayer.EndReached += OnEndReached;
        mediaPlayer.Paused += OnPaused;
        mediaPlayer.Playing += OnPlaying;
    }

    public void StartPlayback(IAudioTrack audioTrack)
    {
        using var media = new Media(vlc, audioTrack.AudioPath, FromType.FromPath);
        mediaPlayer.Media = media;
        mediaPlayer.Play();
    }

    public void TogglePause()
    {
        mediaPlayer.Pause();
    }

    private void OnPlaybackPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs args)
    {
        PlaybackPositionChanged?.Invoke(args.Position);
    }

    private void OnPlaybackStarted(object? sender, EventArgs args)
    {
        PlaybackStarted?.Invoke();
    }

    private void OnEndReached(object? sender, EventArgs args)
    {
        PlaybackFinished?.Invoke();
    }

    private void OnPaused(object? sender, EventArgs args)
    {
        PlaybackPaused?.Invoke();
    }

    private void OnPlaying(object? sender, EventArgs args)
    {
        PlaybackResumed?.Invoke();
    }
}
