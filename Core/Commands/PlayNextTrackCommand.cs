namespace Core.Commands;

public class PlayNextTrackCommand : IPlayNextTrackCommand
{
    private readonly AudioPlayer audioPlayer;
    private readonly IPlayAudioTrackCommand playAudioTrackCommand;

    public PlayNextTrackCommand(AudioPlayer audioPlayer, IPlayAudioTrackCommand playAudioTrackCommand)
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
