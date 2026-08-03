using Core.Playback;
using Core.Playlists;

namespace Nexora.ViewModels.Factories;

public interface IAudioTrackVmFactory
{
    public TrackControlVm Create(IPlaylistItem audioTrack, IAudioPlayer audioPlayer);
}
