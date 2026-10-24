using System.Globalization;
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
    private readonly Lock fileLock = new Lock();
    private int volume;
    private bool isFileReleased;
    private bool isPauseRequested;

    public event Action<IPlaylistItem>? PlaybackStarted;
    public event Action<float>? PlaybackPositionChanged;
    public event Action? PlaybackPaused;
    public event Action? PlaybackFinished;
    public event Action? PlaybackResumed;
    public event Action? PlaybackStopped;
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
        mediaPlayer.Playing += (_, _) => OnPlaying();
        mediaPlayer.Playing += (_, _) => PlaybackResumed?.Invoke();
        mediaPlayer.EndReached += (_, _) => PlaybackFinished?.Invoke();
        mediaPlayer.Paused += (_, _) => PlaybackPaused?.Invoke();
        mediaPlayer.VolumeChanged += (_, args) => OnVolumeChanged(args.Volume * 100f);
        mediaPlayer.Muted += (_, _) => MuteChanged?.Invoke(Mute);
        mediaPlayer.Unmuted += (_, _) => MuteChanged?.Invoke(Mute);
    }

    public void PlayTrack(IPlaylistItem playlistItem)
    {
        lock(fileLock)
        {
            NowPlaying = playlistItem;
            isFileReleased = false;
            Open(playlistItem, 0, false);
        }
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
        if(NowPlaying == null)
        {
            return;
        }

        bool isPaused;

        lock(fileLock)
        {
            isPauseRequested = !isPauseRequested;
            isPaused = isPauseRequested;

            // MediaPlayer.Pause() toggles the state LibVLC reports, which lags behind a quick second toggle.
            if(!isFileReleased)
            {
                mediaPlayer.SetPause(isPaused);
                return;
            }
        }

        if(!isPaused)
        {
            PlaybackResumed?.Invoke();
        }
        else
        {
            PlaybackPaused?.Invoke();
        }
    }

    public void Stop()
    {
        lock(fileLock)
        {
            NowPlaying = null;
            isFileReleased = false;
            mediaPlayer.Stop();
        }

        PlaybackStopped?.Invoke();
    }

    public async Task ReleaseFileAsync(Func<Task> action)
    {
        IPlaylistItem? releasedItem = null;
        long time = 0;

        lock(fileLock)
        {
            var state = mediaPlayer.State;
            bool holdsFile = state == VLCState.Playing || state == VLCState.Paused ||
                state == VLCState.Opening || state == VLCState.Buffering;

            if(NowPlaying != null && holdsFile && !isFileReleased)
            {
                releasedItem = NowPlaying;
                time = Math.Max(0, mediaPlayer.Time);
                isFileReleased = true;
                mediaPlayer.Stop();
            }
        }

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            if(releasedItem != null)
            {
                lock(fileLock)
                {
                    // Playing another track or stopping meanwhile already replaced the released one.
                    if(isFileReleased && NowPlaying == releasedItem)
                    {
                        isFileReleased = false;
                        Open(releasedItem, time, isPauseRequested);
                    }
                }
            }
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

    private void Open(IPlaylistItem playlistItem, long startTimeMs, bool startPaused)
    {
        using var media = new Media(vlc, playlistItem.AudioTrack.AudioPath, FromType.FromPath);

        if(startTimeMs > 0)
        {
            double seconds = startTimeMs / 1000.0;
            media.AddOption($":start-time={seconds.ToString(CultureInfo.InvariantCulture)}");
        }

        if(startPaused)
        {
            media.AddOption(":start-paused");
        }

        isPauseRequested = startPaused;
        mediaPlayer.Media = media;
        mediaPlayer.Play();
    }

    private void OnPlaying()
    {
        var playlistItem = NowPlaying;

        if(playlistItem != null)
        {
            PlaybackStarted?.Invoke(playlistItem);
        }
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
