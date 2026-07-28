using Core.Integrations;
using Core.Playback;
using Core.Playlists;

namespace Core.Commands;

public class PlayTrackCommand : IPlayTrackCommand
{
    private readonly IAudioPlayer audioPlayer;
    private readonly IRichPresenceService richPresenceService;

    public PlayTrackCommand(IAudioPlayer audioPlayer, IRichPresenceService richPresenceService)
    {
        this.audioPlayer = audioPlayer;
        this.richPresenceService = richPresenceService;
    }

    public void Execute(IPlaylistItem playlistItem)
    {
        if(audioPlayer.NowPlaying == playlistItem)
        {
            audioPlayer.TogglePause();
        }
        else
        {
            audioPlayer.PlayTrack(playlistItem);
            richPresenceService.UpdateStatus(playlistItem.AudioTrack.Metadata.Title, playlistItem.AudioTrack.Metadata.Artists);
        }
    }
}
