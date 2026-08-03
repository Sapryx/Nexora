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
    
    [ObservableProperty]
    public partial int Volume { get; set; }

    public bool IsChangingVolume { get; set; }
    
    private readonly IPauseTrackCommand pauseTrackCommand;
    private readonly IPlayNextTrackCommand playNextTrackCommand;
    private readonly IPlayPreviousTrackCommand playPreviousTrackCommand;
    private readonly IChangeVolumeCommand changeVolumeCommand;

    public PlaybackVm(
        IPauseTrackCommand pauseTrackCommand, 
        IPlayNextTrackCommand playNextTrackCommand,
        IPlayPreviousTrackCommand playPreviousTrackCommand,
        IAudioPlayer audioPlayer,
        IChangeVolumeCommand changeVolumeCommand)
    {
        this.pauseTrackCommand = pauseTrackCommand;
        this.playNextTrackCommand = playNextTrackCommand;
        this.playPreviousTrackCommand = playPreviousTrackCommand;
        this.changeVolumeCommand = changeVolumeCommand;

        Volume = audioPlayer.Volume;

        audioPlayer.PlaybackPaused += () => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
        });
        
        audioPlayer.PlaybackResumed += () => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = true;
        });

        audioPlayer.VolumeChanged += newVolume => Dispatcher.UIThread.Post(() =>
        {
            if(!IsChangingVolume)
            {
                Volume = (int)newVolume;
            }
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
    
    partial void OnVolumeChanged(int value)
    {
        changeVolumeCommand.Execute(value);
    }
}
