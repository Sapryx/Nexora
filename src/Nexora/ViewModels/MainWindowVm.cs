using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Commands;
using Core.Logging;
using Core.Playback;
using Core.Playlists;
using Microsoft.Extensions.Logging;
using Nexora.ViewModels.Factories;

namespace Nexora.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    public ObservableCollection<AudioTrackVm> DisplayedAudioTrackVms { get; }

    [ObservableProperty]
    public partial bool IsSeeking { get; set; }

    [ObservableProperty]
    public partial int AudioVolume { get; set; }

    [ObservableProperty]
    public partial float PlaybackPosition { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; }

    [ObservableProperty]
    public partial string PauseButtonText { get; set; }

    [ObservableProperty]
    public partial bool IsCompact { get; set; }
    
    private Dictionary<IAudioTrack, AudioTrackVm> AudioTrackVms { get; }
    private readonly IAudioTrackVmFactory audioTrackVmFactory;
    private readonly IAudioPlayer audioPlayer;
    private readonly PlaylistRegistry playlistRegistry;
    private readonly IChangeVolumeCommand changeVolumeCommand;
    private readonly IPlayNextTrackCommand playNextTrackCommand;
    private readonly IPauseTrackCommand pauseTrackCommand;
    private readonly IPlayPreviousTrackCommand playPreviousTrackCommand;
    private readonly ILogger logger;

    public MainWindowVm(
        IAudioTrackVmFactory audioTrackVmFactory,
        IChangeVolumeCommand changeVolumeCommand,
        IAudioPlayer audioPlayer,
        IPlayNextTrackCommand playNextTrackCommand,
        PlaylistRegistry playlistRegistry,
        IPauseTrackCommand pauseTrackCommand,
        IPlayPreviousTrackCommand playPreviousTrackCommand,
        ILogger<MainWindowVm> logger)
    {
        this.audioTrackVmFactory = audioTrackVmFactory;
        this.changeVolumeCommand = changeVolumeCommand;
        this.audioPlayer = audioPlayer;
        this.playNextTrackCommand = playNextTrackCommand;
        this.playlistRegistry = playlistRegistry;
        this.pauseTrackCommand = pauseTrackCommand;
        this.playPreviousTrackCommand = playPreviousTrackCommand;
        this.logger = logger;

        AudioTrackVms = [];
        DisplayedAudioTrackVms = [];
        AudioVolume = audioPlayer.Volume;
        PlaybackPosition = audioPlayer.PlaybackPosition;
        SearchQuery = "";
        PauseButtonText = "||";
    }

    public void Initialize()
    {
        audioPlayer.PlaybackPositionChanged += value =>
        {
            if(!IsSeeking)
            {
                Dispatcher.UIThread.Post(() => PlaybackPosition = value);
            }
        };

        audioPlayer.PlaybackFinished += () =>
        {
            Dispatcher.UIThread.Post(playNextTrackCommand.Execute);
        };

        audioPlayer.PlaybackPaused += () =>
        {
            Dispatcher.UIThread.Post(() => PauseButtonText = ">");
        };

        audioPlayer.PlaybackResumed += () =>
        {
            Dispatcher.UIThread.Post(() => PauseButtonText = "||");
        };

        playlistRegistry.GlobalPlaylist.ItemAdded += playlistItem =>
        {
            var trackVm = AddAudioTrackVm(playlistItem);

            if(ShouldBeDisplayed(playlistItem.AudioTrack, SearchQuery))
            {
                DisplayedAudioTrackVms.Add(trackVm);
            }
        };
        
        logger.Info($"Application initialized");
    }

    public void UpdateLayout(double windowWidth)
    {
        IsCompact = windowWidth <= 768;
    }

    private AudioTrackVm AddAudioTrackVm(IPlaylistItem playlistItem)
    {
        var audioTrackVm = audioTrackVmFactory.Create(playlistItem, audioPlayer);
        AudioTrackVms[playlistItem.AudioTrack] = audioTrackVm;

        return audioTrackVm;
    }

    partial void OnAudioVolumeChanged(int value)
    {
        changeVolumeCommand.Execute(value);
    }

    partial void OnPlaybackPositionChanged(float value)
    {
        if(IsSeeking)
        {
            audioPlayer.PlaybackPosition = value;
        }
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

    [RelayCommand]
    public void PressPauseButton()
    {
        pauseTrackCommand.Execute();
    }

    [RelayCommand]
    public void PressNextTrackButton()
    {
        playNextTrackCommand.Execute();
    }

    [RelayCommand]
    public void PressPreviousTrackButton()
    {
        playPreviousTrackCommand.Execute();
    }
}
