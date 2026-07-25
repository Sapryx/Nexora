using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Core;
using Core.Commands;
using Gui.ViewModels.Factories;

namespace Gui.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    public ObservableCollection<AudioTrackVm> AudioTrackVms { get; } = [];
    
    [ObservableProperty]
    public partial bool IsSeeking { get; set; }

    [ObservableProperty]
    public partial int AudioVolume { get; set; }

    [ObservableProperty]
    public partial float PlaybackPosition { get; set; }
    
    [ObservableProperty]
    public partial string SearchQuery { get; set; }

    private readonly IAudioTrackVmFactory audioTrackVmFactory;
    private readonly IChangeAudioVolumeCommand changeAudioVolumeCommand;
    private readonly AudioPlayer audioPlayer;
    private readonly IPlayNextAudioTrackCommand playNextAudioTrackCommand;
    private readonly PlaylistRegistry playlistRegistry;

    public MainWindowVm(
        IAudioTrackVmFactory audioTrackVmFactory,
        IChangeAudioVolumeCommand changeAudioVolumeCommand,
        AudioPlayer audioPlayer, 
        IPlayNextAudioTrackCommand playNextAudioTrackCommand,
        PlaylistRegistry playlistRegistry)
    {
        this.audioTrackVmFactory = audioTrackVmFactory;
        this.changeAudioVolumeCommand = changeAudioVolumeCommand;
        this.audioPlayer = audioPlayer;
        this.playNextAudioTrackCommand = playNextAudioTrackCommand;
        this.playlistRegistry = playlistRegistry;

        AudioVolume = audioPlayer.Volume;
        PlaybackPosition = audioPlayer.PlaybackPosition;
        SearchQuery = "";
        
        audioPlayer.PlaybackPositionChanged += value =>
        {
            if(!IsSeeking)
            {
                PlaybackPosition = value;
            }
        };

        audioPlayer.PlaybackFinished += () =>
        {
            Dispatcher.UIThread.Post(playNextAudioTrackCommand.Execute);
        };
        
        SetPlaylist(playlistRegistry.GlobalPlaylist);
    }

    private void SetPlaylist(Playlist playlist)
    {
        foreach(var item in playlist)
        {
            var audioTrackVm = audioTrackVmFactory.Create(item, audioPlayer);
            AudioTrackVms.Add(audioTrackVm);
        }
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
}
