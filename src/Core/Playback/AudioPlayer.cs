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
        get
        {
            if(isAtEnd)
            {
                return endSeekPosition ?? 1;
            }

            var open = pendingOpen;

            if(open != null)
            {
                return PositionOf(open);
            }

            return isPositionReset ? 0 : mediaEngine.Position;
        }
        set
        {
            float? seekPosition = null;

            lock(fileLock)
            {
                isPositionReset = false;

                if(isAtEnd || pendingOpen != null)
                {
                    seekPosition = Math.Clamp(value, 0, 1);
                    SeekClosedTrack(seekPosition.Value);
                }
                else
                {
                    mediaEngine.Position = value;
                }
            }

            if(seekPosition != null)
            {
                PlaybackPositionChanged?.Invoke(seekPosition.Value);
            }
        }
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
    private readonly Dictionary<string, int> releaseCounts = [];
    private PendingOpen? pendingOpen;
    private bool isAtEnd;
    private float? endSeekPosition;
    private bool isPositionReset;

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

        mediaEngine.PositionChanged += OnPositionChanged;
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
            state = PlaybackState.Playing(playlistItem);
            State = state;
            isPositionReset = true;
            OpenWhenReleased(playlistItem, 0);
        }

        StateChanged?.Invoke(state);
        PlaybackPositionChanged?.Invoke(0);
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
                OpenWhenReleased(track, EndSeekTimeMs(track));
            }
            else if(pendingOpen == null)
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
            pendingOpen = null;
            ClearEnd();
            mediaEngine.Stop();
        }

        StateChanged?.Invoke(PlaybackState.Stopped);
    }

    public async Task ReleaseFileAsync(string audioPath, Func<Task> action)
    {
        lock(fileLock)
        {
            int count = releaseCounts.GetValueOrDefault(audioPath);
            releaseCounts[audioPath] = count + 1;
            var track = State.Track;

            if(track != null && track.AudioTrack.AudioPath == audioPath && mediaEngine.HoldsFile)
            {
                pendingOpen = new PendingOpen() { PlaylistItem = track, Time = Math.Max(0, mediaEngine.Time) };
                mediaEngine.Stop();
            }
        }

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            lock(fileLock)
            {
                FinishRelease(audioPath);
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

    private void FinishRelease(string audioPath)
    {
        int count = releaseCounts[audioPath] - 1;

        if(count > 0)
        {
            releaseCounts[audioPath] = count;
            return;
        }

        releaseCounts.Remove(audioPath);

        if(pendingOpen != null && pendingOpen.PlaylistItem.AudioTrack.AudioPath == audioPath)
        {
            var open = pendingOpen;
            pendingOpen = null;
            Open(open.PlaylistItem, open.Time, State.Status == PlaybackStatus.Paused);
        }
    }

    private void OpenWhenReleased(IPlaylistItem playlistItem, long startTimeMs)
    {
        pendingOpen = null;
        ClearEnd();

        if(!releaseCounts.ContainsKey(playlistItem.AudioTrack.AudioPath))
        {
            Open(playlistItem, startTimeMs, false);
            return;
        }

        pendingOpen = new PendingOpen() { PlaylistItem = playlistItem, Time = startTimeMs };
        mediaEngine.Stop();
    }

    private void Open(IPlaylistItem playlistItem, long startTimeMs, bool startPaused)
    {
        var audioTrack = playlistItem.AudioTrack;
        var loudness = audioTrack.Metadata.Loudness;
        double gain = loudness != null ? LoudnessGain.Calculate(loudness, LoudnessGain.TargetLufs) : 0;

        mediaEngine.Open(audioTrack.AudioPath, startTimeMs, startPaused, gain);
        logger.Debug($"Applied loudness gain {gain:0.00} dB to {audioTrack.AudioPath}");
    }

    private void OnPositionChanged(float position)
    {
        isPositionReset = false;
        PlaybackPositionChanged?.Invoke(position);
    }

    private void OnEndReached()
    {
        var nextTrack = PauseOrGetNextTrack();

        if(nextTrack != null)
        {
            PlayTrack(nextTrack);
        }
    }

    private IPlaylistItem? PauseOrGetNextTrack()
    {
        PlaybackState? state;

        lock(fileLock)
        {
            var nextTrack = State.Track?.GetNext();

            if(nextTrack != null)
            {
                return nextTrack;
            }

            state = PauseAtEnd();
        }

        if(state != null)
        {
            StateChanged?.Invoke(state);
        }

        return null;
    }

    private PlaybackState? PauseAtEnd()
    {
        var track = State.Track;

        if(track == null)
        {
            return null;
        }

        isAtEnd = true;
        State = PlaybackState.Paused(track);

        return State;
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
            float position = isAtEnd || pendingOpen != null ? PlaybackPosition : mediaEngine.Position;
            PlaybackPosition = position + normalizedAmount;
        }
    }

    private long EndSeekTimeMs(IPlaylistItem track)
    {
        float position = endSeekPosition ?? 1;

        return position < 1 ? TimeMs(track, position) : 0;
    }

    private void SeekClosedTrack(float position)
    {
        if(pendingOpen != null)
        {
            pendingOpen.Time = TimeMs(pendingOpen.PlaylistItem, position);
        }
        else
        {
            endSeekPosition = position;
        }
    }

    private void ClearEnd()
    {
        isAtEnd = false;
        endSeekPosition = null;
    }

    private static long TimeMs(IPlaylistItem track, float position)
    {
        return (long)Math.Round(track.AudioTrack.Metadata.Duration.TotalMilliseconds * position);
    }

    private static float PositionOf(PendingOpen open)
    {
        double durationMs = open.PlaylistItem.AudioTrack.Metadata.Duration.TotalMilliseconds;

        return durationMs > 0 ? (float)(open.Time / durationMs) : 0;
    }

    private class PendingOpen
    {
        public required IPlaylistItem PlaylistItem { get; init; }
        public required long Time { get; set; }
    }
}
