using Core.Playback;

namespace Core.Commands;

public class PlayNextTrackCommand : IPlayNextTrackCommand
{
    private readonly IAudioPlayer audioPlayer;
    private readonly IPlayTrackCommand playTrackCommand;

    public PlayNextTrackCommand(IAudioPlayer audioPlayer, IPlayTrackCommand playTrackCommand)
    {
        this.audioPlayer = audioPlayer;
        this.playTrackCommand = playTrackCommand;
    }

    public void Execute()
    {
        var nextTrack = audioPlayer.NowPlaying?.GetNext();
        
        if(nextTrack != null)
        {
            playTrackCommand.Execute(nextTrack);
        }
    }
}
