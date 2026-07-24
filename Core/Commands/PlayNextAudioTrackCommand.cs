namespace Core.Commands;

public class PlayNextAudioTrackCommand : IPlayNextAudioTrackCommand
{
    private readonly AudioPlayer audioPlayer;
    private readonly IPlayAudioTrackCommand playAudioTrackCommand;

    public PlayNextAudioTrackCommand(AudioPlayer audioPlayer, IPlayAudioTrackCommand playAudioTrackCommand)
    {
        this.audioPlayer = audioPlayer;
        this.playAudioTrackCommand = playAudioTrackCommand;
    }

    public void Execute()
    {
        var nextTrack = audioPlayer.NowPlaying?.GetNext();
        
        if(nextTrack != null)
        {
            playAudioTrackCommand.Execute(nextTrack);
        }
    }
}
