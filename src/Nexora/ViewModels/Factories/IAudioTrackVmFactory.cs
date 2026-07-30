using Core.Playback;
using Core.Playlists;

namespace Nexora.ViewModels.Factories;

public interface IAudioTrackVmFactory
{
    public AudioTrackVm Create(IPlaylistItem audioTrack, IAudioPlayer audioPlayer);
}
