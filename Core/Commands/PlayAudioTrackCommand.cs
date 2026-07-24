namespace Core.Commands;

public class PlayAudioTrackCommand : IPlayAudioTrackCommand
{
    private readonly AudioPlayer audioPlayer;
    private readonly IRpcService rpcService;

    public PlayAudioTrackCommand(AudioPlayer audioPlayer, IRpcService rpcService)
    {
        this.audioPlayer = audioPlayer;
        this.rpcService = rpcService;
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
            rpcService.UpdateStatus(playlistItem.AudioTrack.Metadata.Title, string.Join(", ", playlistItem.AudioTrack.Metadata.Artists));
        }
    }
}
