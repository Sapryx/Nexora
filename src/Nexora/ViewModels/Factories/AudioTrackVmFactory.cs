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
    
    public AudioTrackVm Create(IPlaylistItem playlistItem, IAudioPlayer audioPlayer)
    {
        return new AudioTrackVm(playlistItem, toggleTrackCommand, audioPlayer);
    }
}
