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

    public MainWindowVm(
        IAudioTrackVmFactory audioTrackVmFactory,
        IChangeAudioVolumeCommand changeAudioVolumeCommand,
        AudioPlayer audioPlayer, IPlayNextAudioTrackCommand playNextAudioTrackCommand)
    {
        this.audioTrackVmFactory = audioTrackVmFactory;
        this.changeAudioVolumeCommand = changeAudioVolumeCommand;
        this.audioPlayer = audioPlayer;
        this.playNextAudioTrackCommand = playNextAudioTrackCommand;

        this.AudioVolume = audioPlayer.Volume;
        this.PlaybackPosition = audioPlayer.PlaybackPosition;
        this.SearchQuery = "";

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
    }

    public void SetPlaylist(Playlist playlist)
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
