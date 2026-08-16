using Core.Commands;
using Core.Playback;
using Core.Playlists;

namespace Nexora.ViewModels.Factories;

public class AudioTrackVmFactory : IAudioTrackVmFactory
{
    private readonly IToggleTrackCommand toggleTrackCommand;

    public AudioTrackVmFactory(IToggleTrackCommand toggleTrackCommand)
    {
        this.toggleTrackCommand = toggleTrackCommand;
    }
    
    public TrackControlVm Create(IPlaylistItem playlistItem, IAudioPlayer audioPlayer)
    {
        var vm = new TrackControlVm(toggleTrackCommand, audioPlayer);
        _ = vm.SetTrack(playlistItem);
        
        return vm;
    }
}
