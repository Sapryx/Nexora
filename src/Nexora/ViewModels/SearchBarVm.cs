using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Playback;
using Core.Playlists;
using Core.Search;
using Nexora.Threading;
using Nexora.ViewModels.Factories;

namespace Nexora.ViewModels;

public partial class SearchBarVm : ViewModelBase
{
    private readonly ITrackControlVmFactory trackControlVmFactory;
    private readonly KeyboardLayoutTranslator layoutTranslator;
    private TrackSearchQuery searchQuery;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    public Dictionary<IAudioTrack, TrackControlVm> AudioTrackVms { get; } = [];
    public ObservableCollection<TrackControlVm> DisplayedAudioTrackVms { get; } = [];

    public SearchBarVm(
        PlaylistRegistry playlistRegistry,
        ITrackControlVmFactory trackControlVmFactory,
        KeyboardLayoutTranslator layoutTranslator,
        IUiDispatcher uiDispatcher)
    {
        this.trackControlVmFactory = trackControlVmFactory;
        this.layoutTranslator = layoutTranslator;
        searchQuery = new TrackSearchQuery("", layoutTranslator);

        playlistRegistry.GlobalPlaylist.ItemAdded += playlistItem => uiDispatcher.Post(() =>
        {
            var trackVm = AddAudioTrackVm(playlistItem);

            if(searchQuery.Matches(playlistItem.AudioTrack))
            {
                DisplayedAudioTrackVms.Add(trackVm);
            }
        });
    }
    
    private TrackControlVm AddAudioTrackVm(IPlaylistItem playlistItem)
    {
        var audioTrackVm = trackControlVmFactory.Create(playlistItem);
        AudioTrackVms[playlistItem.AudioTrack] = audioTrackVm;

        return audioTrackVm;
    }
    
    partial void OnSearchQueryChanged(string value)
    {
        searchQuery = new TrackSearchQuery(value, layoutTranslator);
        DisplayedAudioTrackVms.Clear();

        foreach(var (audioTrack, audioTrackVm) in AudioTrackVms)
        {
            if(searchQuery.Matches(audioTrack))
            {
                DisplayedAudioTrackVms.Add(audioTrackVm);
            }
        }
    }
}
