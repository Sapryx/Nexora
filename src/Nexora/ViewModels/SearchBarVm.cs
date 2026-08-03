using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Playback;
using Core.Playlists;
using Nexora.ViewModels.Factories;

namespace Nexora.ViewModels;

public partial class SearchBarVm : ViewModelBase
{
    private readonly IAudioTrackVmFactory audioTrackVmFactory;
    private readonly IAudioPlayer audioPlayer;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    public Dictionary<IAudioTrack, TrackControlVm> AudioTrackVms { get; } = [];
    public ObservableCollection<TrackControlVm> DisplayedAudioTrackVms { get; } = [];

    public SearchBarVm(
        PlaylistRegistry playlistRegistry,
        IAudioTrackVmFactory audioTrackVmFactory,
        IAudioPlayer audioPlayer)
    {
        this.audioTrackVmFactory = audioTrackVmFactory;
        this.audioPlayer = audioPlayer;
        
        playlistRegistry.GlobalPlaylist.ItemAdded += playlistItem =>
        {
            var trackVm = AddAudioTrackVm(playlistItem);

            if(ShouldBeDisplayed(playlistItem.AudioTrack, SearchQuery))
            {
                DisplayedAudioTrackVms.Add(trackVm);
            }
        };
    }
    
    private TrackControlVm AddAudioTrackVm(IPlaylistItem playlistItem)
    {
        var audioTrackVm = audioTrackVmFactory.Create(playlistItem, audioPlayer);
        AudioTrackVms[playlistItem.AudioTrack] = audioTrackVm;

        return audioTrackVm;
    }
    
    partial void OnSearchQueryChanged(string value)
    {
        string rawQuery = value;
        
        DisplayedAudioTrackVms.Clear();

        if(string.IsNullOrEmpty(rawQuery.Trim()))
        {
            foreach(var audioTrackVm in AudioTrackVms.Values)
            {
                DisplayedAudioTrackVms.Add(audioTrackVm);
            }

            return;
        }

        foreach(var audioTrack in AudioTrackVms.Keys)
        {
            if(ShouldBeDisplayed(audioTrack, rawQuery))
            {
                var audioTrackVm = AudioTrackVms[audioTrack];
                DisplayedAudioTrackVms.Add(audioTrackVm);
            }
        }
    }
    
    private bool ShouldBeDisplayed(IAudioTrack audioTrack, string rawQuery)
    {
        string query = rawQuery.Trim().ToLower();
        string title = audioTrack.Metadata.Title.ToLower();
        string artists = audioTrack.Metadata.Artists.ToLower();
        bool titleMatches = title.Contains(query);
        bool artistsMatch = artists.Contains(query);

        return titleMatches || artistsMatch;
    }
}
