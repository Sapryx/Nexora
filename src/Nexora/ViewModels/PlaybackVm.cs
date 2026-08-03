using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Commands;
using Core.Playback;

namespace Nexora.ViewModels;

public partial class PlaybackVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsPlaying { get; private set; }
    
    private readonly IPauseTrackCommand pauseTrackCommand;
    private readonly IPlayNextTrackCommand playNextTrackCommand;
    private readonly IPlayPreviousTrackCommand playPreviousTrackCommand;

    public PlaybackVm(
        IPauseTrackCommand pauseTrackCommand, 
        IPlayNextTrackCommand playNextTrackCommand,
        IPlayPreviousTrackCommand playPreviousTrackCommand,
        IAudioPlayer audioPlayer)
    {
        this.pauseTrackCommand = pauseTrackCommand;
        this.playNextTrackCommand = playNextTrackCommand;
        this.playPreviousTrackCommand = playPreviousTrackCommand;

        audioPlayer.PlaybackPaused += () => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
        });
        
        audioPlayer.PlaybackResumed += () => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = true;
        });
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
