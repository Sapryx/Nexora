namespace Core.Commands;

public interface IPlayAudioTrackCommand
{
    public void Execute(PlaylistItem playlistItem);
}
