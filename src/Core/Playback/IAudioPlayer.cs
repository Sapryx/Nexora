using Core.Playlists;

namespace Core.Playback;

public interface IAudioPlayer
{
    public PlaybackState State { get; }
    public int Volume { get; set; }
    public float PlaybackPosition { get; set; }
    public bool Mute { get; set; }

    public event Action<PlaybackState>? StateChanged;
    public event Action<float>? PlaybackPositionChanged;
    public event Action<float>? VolumeChanged;
    public event Action<bool>? MuteChanged;

    public void PlayTrack(IPlaylistItem playlistItem);
    public void PlayNextTrack();
    public void PlayPreviousTrack();
    public void TogglePause();
    public void Stop();
    public void SkipForward();
    public void SkipBack();

    /// <summary>
    /// Closes the playing track's file while <paramref name="action"/> runs if it is <paramref name="audioPath"/>,
    /// then reopens it at the same time once the last overlapping release of it finishes.
    /// Pausing or resuming meanwhile changes whether it reopens playing or paused.
    /// </summary>
    public Task ReleaseFileAsync(string audioPath, Func<Task> action);
}
