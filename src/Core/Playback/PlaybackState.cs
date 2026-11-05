using Core.Playlists;

namespace Core.Playback;

public class PlaybackState
{
    public static PlaybackState Stopped { get; } = new PlaybackState(PlaybackStatus.Stopped, null);
    public PlaybackStatus Status { get; }
    public IPlaylistItem? Track { get; }

    private PlaybackState(PlaybackStatus status, IPlaylistItem? track)
    {
        Status = status;
        Track = track;
    }

    public static PlaybackState Playing(IPlaylistItem track)
    {
        return new PlaybackState(PlaybackStatus.Playing, track);
    }

    public static PlaybackState Paused(IPlaylistItem track)
    {
        return new PlaybackState(PlaybackStatus.Paused, track);
    }
}
