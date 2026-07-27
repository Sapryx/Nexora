using Core;
using Core.Commands;

namespace Gui.ViewModels.Factories;

public class AudioTrackVmFactory : IAudioTrackVmFactory
{
    private readonly IPlayTrackCommand playTrackCommand;

    public AudioTrackVmFactory(IPlayTrackCommand playTrackCommand)
    {
        this.playTrackCommand = playTrackCommand;
    }
    
    public AudioTrackVm Create(PlaylistItem playlistItem, AudioPlayer audioPlayer)
    {
        return new AudioTrackVm(playlistItem, playTrackCommand, audioPlayer);
    }
}
