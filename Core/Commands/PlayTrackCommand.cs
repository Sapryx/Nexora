using Core.Integrations;
using Core.Playback;
using Core.Playlists;

namespace Core.Commands;

public class PlayTrackCommand : IPlayTrackCommand
{
    private readonly AudioPlayer audioPlayer;
    private readonly IRichPresenceService richPresenceService;

    public PlayTrackCommand(AudioPlayer audioPlayer, IRichPresenceService richPresenceService)
    {
        this.audioPlayer = audioPlayer;
        this.richPresenceService = richPresenceService;
    }

    public void Execute(PlaylistItem playlistItem)
    {
        if(audioPlayer.NowPlaying == playlistItem)
        {
            audioPlayer.TogglePause();
        }
        else
        {
            audioPlayer.PlayTrack(playlistItem);
            richPresenceService.UpdateStatus(playlistItem.AudioTrack.Metadata.Title, string.Join(", ", playlistItem.AudioTrack.Metadata.Artists));
        }
    }
}
