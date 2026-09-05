using Core.Playback;
using Core.Playlists;

namespace Nexora.ViewModels.Factories;

public class AudioTrackVmFactory : IAudioTrackVmFactory
{
    public TrackControlVm Create(IPlaylistItem playlistItem, IAudioPlayer audioPlayer)
    {
        var vm = new TrackControlVm(audioPlayer);
        _ = vm.SetTrack(playlistItem);
        
        return vm;
    }
}
