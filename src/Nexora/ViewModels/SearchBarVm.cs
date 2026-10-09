using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Playback;
using Core.Playlists;
using Nexora.Threading;
using Nexora.ViewModels.Factories;

namespace Nexora.ViewModels;

public partial class SearchBarVm : ViewModelBase
{
    private readonly ITrackControlVmFactory trackControlVmFactory;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    public Dictionary<IAudioTrack, TrackControlVm> AudioTrackVms { get; } = [];
    public ObservableCollection<TrackControlVm> DisplayedAudioTrackVms { get; } = [];

    public SearchBarVm(
        PlaylistRegistry playlistRegistry,
        ITrackControlVmFactory trackControlVmFactory,
        IUiDispatcher uiDispatcher)
    {
        this.trackControlVmFactory = trackControlVmFactory;
        
        playlistRegistry.GlobalPlaylist.ItemAdded += playlistItem => uiDispatcher.Post(() =>
        {
            var trackVm = AddAudioTrackVm(playlistItem);

            if(ShouldBeDisplayed(playlistItem.AudioTrack, SearchQuery))
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
        string rawQuery = value;
        string query = rawQuery.Trim().ToLower();

        DisplayedAudioTrackVms.Clear();

        if(string.IsNullOrEmpty(query))
        {
            foreach(var audioTrackVm in AudioTrackVms.Values)
            {
                DisplayedAudioTrackVms.Add(audioTrackVm);
            }

            return;
        }

        foreach(var audioTrack in AudioTrackVms.Keys)
        {
            if(ShouldBeDisplayed(audioTrack, query))
            {
                var audioTrackVm = AudioTrackVms[audioTrack];
                DisplayedAudioTrackVms.Add(audioTrackVm);
            }
        }
    }

    private bool ShouldBeDisplayed(IAudioTrack audioTrack, string query)
    {
        string title = audioTrack.Metadata.Title.ToLower();
        string artists = audioTrack.Metadata.Artists.ToLower();
        bool titleMatches = title.Contains(query);
        bool artistsMatch = artists.Contains(query);

        return titleMatches || artistsMatch;
    }
}
