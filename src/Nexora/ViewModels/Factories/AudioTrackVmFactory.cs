using Core.Playback;
using Core.Playlists;
using Nexora.Media;

namespace Nexora.ViewModels.Factories;

public class AudioTrackVmFactory : IAudioTrackVmFactory
{
    private readonly ICoverCache coverCache;

    public AudioTrackVmFactory(ICoverCache coverCache)
    {
        this.coverCache = coverCache;
    }

    public TrackControlVm Create(IPlaylistItem playlistItem, IAudioPlayer audioPlayer)
    {
        var vm = new TrackControlVm(audioPlayer, coverCache);
        vm.SetTrack(playlistItem);

        return vm;
    }
}
