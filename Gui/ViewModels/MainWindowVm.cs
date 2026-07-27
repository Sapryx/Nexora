using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core;
using Core.Commands;
using Gui.ViewModels.Factories;

namespace Gui.ViewModels;

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

    private Dictionary<IAudioTrack, AudioTrackVm> AudioTrackVms { get; }
    private readonly IAudioTrackVmFactory audioTrackVmFactory;
    private readonly AudioPlayer audioPlayer;
    private readonly PlaylistRegistry playlistRegistry;
    private readonly IChangeAudioVolumeCommand changeAudioVolumeCommand;
    private readonly IPlayNextAudioTrackCommand playNextAudioTrackCommand;
    private readonly IPauseTrackCommand pauseTrackCommand;
    private readonly IPlayPreviousTrackCommand playPreviousTrackCommand;

    public MainWindowVm(
        IAudioTrackVmFactory audioTrackVmFactory,
        IChangeAudioVolumeCommand changeAudioVolumeCommand,
        AudioPlayer audioPlayer, 
        IPlayNextAudioTrackCommand playNextAudioTrackCommand,
        PlaylistRegistry playlistRegistry,
        IPauseTrackCommand pauseTrackCommand,
        IPlayPreviousTrackCommand playPreviousTrackCommand)
    {
        this.audioTrackVmFactory = audioTrackVmFactory;
        this.changeAudioVolumeCommand = changeAudioVolumeCommand;
        this.audioPlayer = audioPlayer;
        this.playNextAudioTrackCommand = playNextAudioTrackCommand;
        this.playlistRegistry = playlistRegistry;
        this.pauseTrackCommand = pauseTrackCommand;
        this.playPreviousTrackCommand = playPreviousTrackCommand;

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
            Dispatcher.UIThread.Post(playNextAudioTrackCommand.Execute);
        };

        audioPlayer.PlaybackPaused += () =>
        {
            Dispatcher.UIThread.Post(() => PauseButtonText = ">");
        };

        audioPlayer.PlaybackResumed += () =>
        {
            Dispatcher.UIThread.Post(() => PauseButtonText = "||");
        };
        
        SetPlaylist(playlistRegistry.GlobalPlaylist);
    }

    private void SetPlaylist(Playlist playlist)
    {
        DisplayedAudioTrackVms.Clear();
        
        foreach(var item in playlist)
        {
            var audioTrackVm = GetOrCreateAudioTrackVm(item);
            DisplayedAudioTrackVms.Add(audioTrackVm);
        }
    }

    private AudioTrackVm GetOrCreateAudioTrackVm(PlaylistItem playlistItem)
    {
        bool audioTrackVmExists = AudioTrackVms.TryGetValue(playlistItem.AudioTrack, out var audioTrackVm);
        
        if(!audioTrackVmExists)
        {
            audioTrackVm = audioTrackVmFactory.Create(playlistItem, audioPlayer);
            AudioTrackVms[playlistItem.AudioTrack] = audioTrackVm;
        }

        return audioTrackVm!;
    }

    partial void OnAudioVolumeChanged(int value)
    {
        changeAudioVolumeCommand.Execute(value);
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
        string query = value.Trim().ToLower();
        
        if(string.IsNullOrEmpty(query))
        {
            SetPlaylist(playlistRegistry.GlobalPlaylist);
            return;
        }
        
        var queryPlaylist = new Playlist();

        foreach(var item in playlistRegistry.GlobalPlaylist)
        {
            string title = item.AudioTrack.Metadata.Title.ToLower();
            string artists = string.Join(", ", item.AudioTrack.Metadata.Artists).ToLower();
            
            bool titleMatches = title.Contains(query);
            bool artistsMatch = artists.Contains(query);

            if(titleMatches || artistsMatch)
            {
                queryPlaylist.AddTrack(item.AudioTrack);
            }
        }
        
        SetPlaylist(queryPlaylist);
    }

    [RelayCommand]
    public void PressPauseButton()
    {
        pauseTrackCommand.Execute();
    }

    [RelayCommand]
    public void PressNextTrackButton()
    {
        playNextAudioTrackCommand.Execute();
    }

    [RelayCommand]
    public void PressPreviousTrackButton()
    {
        playPreviousTrackCommand.Execute();
    }
}
