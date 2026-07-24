using Core;

namespace Gui.ViewModels.Factories;

public interface IAudioTrackVmFactory
{
    public AudioTrackVm Create(PlaylistItem audioTrack, AudioPlayer audioPlayer);
}
