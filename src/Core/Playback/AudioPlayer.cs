using Core.Logging;
using Core.Playback.Loudness;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging;

namespace Core.Playback;

public class AudioPlayer : IAudioPlayer
{
    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public int Volume
    {
        get => volume;
        set
        {
            volume = value;
            mediaEngine.Volume = value;
            volumeStorage.Save(value);
        }
    }

    public float PlaybackPosition
    {
        get => mediaEngine.Position;
        set => mediaEngine.Position = value;
    }

    public bool Mute
    {
        get => mediaEngine.Mute;
        set => mediaEngine.Mute = value;
    }

    private const int DefaultVolume = 50;
    private readonly ILogger<AudioPlayer> logger;
    private readonly IMediaEngine mediaEngine;
    private readonly IVolumeStorage volumeStorage;
    private readonly Lock fileLock = new Lock();
    private int volume;
    private ReleasedFile? releasedFile;
    private bool isAtEnd;

    public event Action<PlaybackState>? StateChanged;
    public event Action<float>? PlaybackPositionChanged;
    public event Action<float>? VolumeChanged;
    public event Action<bool>? MuteChanged;

    public AudioPlayer(ILogger<AudioPlayer> logger, IMediaEngine mediaEngine, IVolumeStorage volumeStorage)
    {
        this.logger = logger;
        this.mediaEngine = mediaEngine;
        this.volumeStorage = volumeStorage;

        volume = volumeStorage.Load() ?? DefaultVolume;
        mediaEngine.Volume = volume;

        mediaEngine.PositionChanged += position => PlaybackPositionChanged?.Invoke(position);
        mediaEngine.EndReached += OnEndReached;
        mediaEngine.EncounteredError += OnEncounteredError;
        mediaEngine.VolumeChanged += OnVolumeChanged;
        mediaEngine.MuteChanged += isMuted => MuteChanged?.Invoke(isMuted);
    }

    public void PlayTrack(IPlaylistItem playlistItem)
    {
        PlaybackState state;

        lock(fileLock)
        {
            releasedFile = null;
            state = PlaybackState.Playing(playlistItem);
            State = state;
            Open(playlistItem, 0, false);
        }

        StateChanged?.Invoke(state);
    }

    public void PlayNextTrack()
    {
        var nextTrack = State.Track?.GetNext();

        if(nextTrack != null)
        {
            PlayTrack(nextTrack);
        }
    }

    public void PlayPreviousTrack()
    {
        var previousTrack = State.Track?.GetPrevious();

        if(previousTrack != null)
        {
            PlayTrack(previousTrack);
        }
    }

    public void TogglePause()
    {
        PlaybackState state;

        lock(fileLock)
        {
            var track = State.Track;

            if(track == null)
            {
                return;
            }

            state = State.Status == PlaybackStatus.Playing ? PlaybackState.Paused(track) : PlaybackState.Playing(track);
            State = state;

            if(isAtEnd)
            {
                Open(track, 0, false);
            }
            else if(releasedFile == null)
            {
                // MediaPlayer.Pause() toggles the state LibVLC reports, which lags behind a quick second toggle.
                mediaEngine.SetPause(state.Status == PlaybackStatus.Paused);
            }
        }

        StateChanged?.Invoke(state);
    }

    public void Stop()
    {
        lock(fileLock)
        {
            State = PlaybackState.Stopped;
            releasedFile = null;
            mediaEngine.Stop();
        }

        StateChanged?.Invoke(PlaybackState.Stopped);
    }

    public async Task ReleaseFileAsync(string audioPath, Func<Task> action)
    {
        ReleasedFile? release = null;

        lock(fileLock)
        {
            var track = State.Track;

            if(releasedFile != null && releasedFile.PlaylistItem.AudioTrack.AudioPath == audioPath)
            {
                release = releasedFile;
                release.Count++;
            }
            else if(track != null && track.AudioTrack.AudioPath == audioPath && mediaEngine.HoldsFile)
            {
                release = new ReleasedFile() { PlaylistItem = track, Time = Math.Max(0, mediaEngine.Time), Count = 1 };
                releasedFile = release;
                mediaEngine.Stop();
            }
        }

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            if(release != null)
            {
                lock(fileLock)
                {
                    release.Count--;

                    // Playing another track or stopping meanwhile already replaced the released one.
                    if(releasedFile == release && release.Count == 0)
                    {
                        releasedFile = null;
                        Open(release.PlaylistItem, release.Time, State.Status == PlaybackStatus.Paused);
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
        var audioTrack = playlistItem.AudioTrack;
        var loudness = audioTrack.Metadata.Loudness;
        double gain = loudness != null ? LoudnessGain.Calculate(loudness, LoudnessGain.TargetLufs) : 0;

        isAtEnd = false;
        mediaEngine.Open(audioTrack.AudioPath, startTimeMs, startPaused, gain);
        logger.Debug($"Applied loudness gain {gain:0.00} dB to {audioTrack.AudioPath}");
    }

    private void OnEndReached()
    {
        var track = State.Track;
        var nextTrack = track?.GetNext();

        if(nextTrack != null)
        {
            PlayTrack(nextTrack);
            return;
        }

        if(track == null)
        {
            return;
        }

        var state = PlaybackState.Paused(track);

        lock(fileLock)
        {
            isAtEnd = true;
            State = state;
        }

        StateChanged?.Invoke(state);
    }

    private void OnEncounteredError()
    {
        var track = State.Track;

        if(track != null)
        {
            logger.Error($"Failed to play {track.AudioTrack.AudioPath}");
            Stop();
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
        var track = State.Track;

        if(track != null)
        {
            float normalizedAmount = amount / (float)track.AudioTrack.Metadata.Duration.TotalSeconds;
            mediaEngine.Position += normalizedAmount;
        }
    }

    private class ReleasedFile
    {
        public required IPlaylistItem PlaylistItem { get; init; }
        public required long Time { get; init; }
        public required int Count { get; set; }
    }
}
