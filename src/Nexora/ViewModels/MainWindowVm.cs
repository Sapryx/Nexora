using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Commands;
using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Nexora.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsSeeking { get; set; }

    [ObservableProperty]
    public partial int AudioVolume { get; set; }

    [ObservableProperty]
    public partial float PlaybackPosition { get; set; }

    [ObservableProperty]
    public partial string PauseButtonText { get; set; }

    [ObservableProperty]
    public partial bool IsCompact { get; set; }
    
    public SearchBarVm SearchBarVm { get; }

    private readonly IAudioPlayer audioPlayer;
    private readonly IChangeVolumeCommand changeVolumeCommand;
    private readonly IPlayNextTrackCommand playNextTrackCommand;
    private readonly IPauseTrackCommand pauseTrackCommand;
    private readonly IPlayPreviousTrackCommand playPreviousTrackCommand;
    private readonly ILogger logger;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        IChangeVolumeCommand changeVolumeCommand,
        IAudioPlayer audioPlayer,
        IPlayNextTrackCommand playNextTrackCommand,
        IPauseTrackCommand pauseTrackCommand,
        IPlayPreviousTrackCommand playPreviousTrackCommand,
        SearchBarVm searchBarVm)
    {
        this.logger = logger;
        this.changeVolumeCommand = changeVolumeCommand;
        this.audioPlayer = audioPlayer;
        this.playNextTrackCommand = playNextTrackCommand;
        this.pauseTrackCommand = pauseTrackCommand;
        this.playPreviousTrackCommand = playPreviousTrackCommand;
        
        SearchBarVm = searchBarVm;
        AudioVolume = audioPlayer.Volume;
        PlaybackPosition = audioPlayer.PlaybackPosition;
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
        
        logger.Info($"Application initialized");
    }

    public void UpdateLayout(double windowWidth)
    {
        IsCompact = windowWidth <= 768;
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
