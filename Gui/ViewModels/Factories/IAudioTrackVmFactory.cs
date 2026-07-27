using Core;
using Core.Playback;
using Core.Playlists;

namespace Gui.ViewModels.Factories;

public interface IAudioTrackVmFactory
{
    public AudioTrackVm Create(PlaylistItem audioTrack, AudioPlayer audioPlayer);
}
