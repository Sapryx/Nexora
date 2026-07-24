using Core;
using Core.Commands;

namespace Gui.ViewModels.Factories;

public class AudioTrackVmFactory : IAudioTrackVmFactory
{
    private readonly IPlayAudioTrackCommand playAudioTrackCommand;

    public AudioTrackVmFactory(IPlayAudioTrackCommand playAudioTrackCommand)
    {
        this.playAudioTrackCommand = playAudioTrackCommand;
    }
    
    public AudioTrackVm Create(PlaylistItem playlistItem, AudioPlayer audioPlayer)
    {
        return new AudioTrackVm(playlistItem, playAudioTrackCommand, audioPlayer);
    }
}
