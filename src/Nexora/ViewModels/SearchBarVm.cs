using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;
using Core.Playlists;
using Core.Search;
using Nexora.Threading;
using Nexora.ViewModels.Factories;

namespace Nexora.ViewModels;

public partial class SearchBarVm : ViewModelBase
{
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    public Dictionary<IAudioTrack, TrackControlVm> AudioTrackVms { get; } = [];
    public ObservableCollection<TrackControlVm> DisplayedAudioTrackVms { get; } = [];

    private readonly ITrackControlVmFactory trackControlVmFactory;
    private readonly KeyboardLayoutTranslator layoutTranslator;
    private readonly List<TrackMatch> displayedMatches = [];
    private TrackSearchQuery searchQuery;

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
            DisplayIfMatches(playlistItem.AudioTrack, trackVm);
        });
    }

    private TrackControlVm AddAudioTrackVm(IPlaylistItem playlistItem)
    {
        var audioTrackVm = trackControlVmFactory.Create(playlistItem);
        AudioTrackVms[playlistItem.AudioTrack] = audioTrackVm;

        return audioTrackVm;
    }

    [RelayCommand]
    public void ClearSearchQuery()
    {
        SearchQuery = "";
    }

    partial void OnSearchQueryChanged(string value)
    {
        searchQuery = new TrackSearchQuery(value, layoutTranslator);
        DisplayedAudioTrackVms.Clear();
        displayedMatches.Clear();

        foreach(var pair in AudioTrackVms)
        {
            DisplayIfMatches(pair.Key, pair.Value);
        }
    }

    private void DisplayIfMatches(IAudioTrack audioTrack, TrackControlVm audioTrackVm)
    {
        var match = searchQuery.Match(audioTrack);

        if(match == null)
        {
            return;
        }

        int index = FindInsertIndex(match);
        displayedMatches.Insert(index, match);
        DisplayedAudioTrackVms.Insert(index, audioTrackVm);
    }

    private int FindInsertIndex(TrackMatch match)
    {
        int low = 0;
        int high = displayedMatches.Count;

        while(low < high)
        {
            int middle = (low + high) / 2;

            if(displayedMatches[middle].CompareTo(match) <= 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
