using Core.Playlists;
using Core.Storage;
using LibVLCSharp.Shared;

namespace Core.Playback;

public class AudioPlayer : IAudioPlayer
{
    public IPlaylistItem? NowPlaying { get; private set; }
    public bool IsPlaying => mediaPlayer.IsPlaying;

    public int Volume
    {
        get => volume;
        set
        {
            volume = value;
            mediaPlayer.Volume = value;
            volumeStorage.Save(value);
        }
    }

    public float PlaybackPosition
    {
        get => mediaPlayer.Position;
        set => mediaPlayer.Position = value;
    }

    public bool Mute
    {
        get => mediaPlayer.Mute;
        set => mediaPlayer.Mute = value;
    }

    private const int DefaultVolume = 50;
    private readonly LibVLC vlc;
    private readonly MediaPlayer mediaPlayer;
    private readonly IVolumeStorage volumeStorage;
    private int volume;

    public event Action? PlaybackStarted;
    public event Action<float>? PlaybackPositionChanged;
    public event Action? PlaybackPaused;
    public event Action? PlaybackFinished;
    public event Action? PlaybackResumed;
    public event Action<float>? VolumeChanged;
    public event Action<bool>? MuteChanged;

    public AudioPlayer(LibVLC vlc, IVolumeStorage volumeStorage)
    {
        this.vlc = vlc;
        this.volumeStorage = volumeStorage;
        mediaPlayer = new MediaPlayer(vlc);

        volume = volumeStorage.Load() ?? DefaultVolume;
        mediaPlayer.Volume = volume;

        mediaPlayer.PositionChanged += (_, args) => PlaybackPositionChanged?.Invoke(args.Position);
        mediaPlayer.Playing += (_, _) => PlaybackStarted?.Invoke();
        mediaPlayer.Playing += (_, _) => PlaybackResumed?.Invoke();
        mediaPlayer.EndReached += (_, _) => PlaybackFinished?.Invoke();
        mediaPlayer.Paused += (_, _) => PlaybackPaused?.Invoke();
        mediaPlayer.VolumeChanged += (_, args) => OnVolumeChanged(args.Volume * 100f);
        mediaPlayer.Muted += (_, _) => MuteChanged?.Invoke(Mute);
        mediaPlayer.Unmuted += (_, _) => MuteChanged?.Invoke(Mute);
    }

    public void PlayTrack(IPlaylistItem playlistItem)
    {
        NowPlaying = playlistItem;

        using var media = new Media(vlc, playlistItem.AudioTrack.AudioPath, FromType.FromPath);
        mediaPlayer.Media = media;
        mediaPlayer.Play();
    }

    public void PlayNextTrack()
    {
        var nextTrack = NowPlaying?.GetNext();

        if(nextTrack != null)
        {
            PlayTrack(nextTrack);
        }
    }

    public void PlayPreviousTrack()
    {
        var previousTrack = NowPlaying?.GetPrevious();

        if(previousTrack != null)
        {
            PlayTrack(previousTrack);
        }
    }

    public void TogglePause()
    {
        if(NowPlaying != null)
        {
            mediaPlayer.Pause();
        }
    }

    public void SkipForward()
    {
        Skip(5);
    }

    public void SkipBack()
    {
        Skip(-5);
    }

    private void OnVolumeChanged(float newVolume)
    {
        if(newVolume < 0)
        {
            return;
        }

        int roundedVolume = (int)MathF.Round(newVolume);

        if(roundedVolume != volume)
        {
            volume = roundedVolume;
            volumeStorage.Save(roundedVolume);
        }

        VolumeChanged?.Invoke(newVolume);
    }

    private void Skip(float amount)
    {
        if(NowPlaying != null)
        {
            float normalizedAmount = amount / (float)NowPlaying.AudioTrack.Metadata.Duration.TotalSeconds;
            mediaPlayer.Position += normalizedAmount;
        }
    }
}
