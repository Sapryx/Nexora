using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial float PlaybackPosition { get; set; }

    [ObservableProperty]
    public partial bool IsCompact { get; set; }
    
    public SearchBarVm SearchBarVm { get; }
    public PlaybackVm PlaybackVm { get; }

    private readonly IAudioPlayer audioPlayer;
    private readonly IPlayNextTrackCommand playNextTrackCommand;
    private readonly ILogger logger;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        IAudioPlayer audioPlayer,
        IPlayNextTrackCommand playNextTrackCommand,
        SearchBarVm searchBarVm, PlaybackVm playbackVm)
    {
        this.logger = logger;
        this.audioPlayer = audioPlayer;
        this.playNextTrackCommand = playNextTrackCommand;
        
        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
        PlaybackPosition = audioPlayer.PlaybackPosition;
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
        
        logger.Info($"Application initialized");
    }

    public void UpdateLayout(double windowWidth)
    {
        IsCompact = windowWidth <= 768;
    }

    partial void OnPlaybackPositionChanged(float value)
    {
        if(IsSeeking)
        {
            audioPlayer.PlaybackPosition = value;
        }
    }
}
