using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;

namespace Nexora.ViewModels;

public partial class PlaybackVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsPlaying { get; private set; }
    
    [ObservableProperty]
    public partial float PlaybackPosition { get; set; }

    [ObservableProperty]
    public partial TrackViewVm? PlayingTrackViewVm { get; set; }
    
    [ObservableProperty]
    public partial int Volume { get; set; }

    public bool IsChangingVolume { get; set; }
    public bool IsSeeking { get; set; }
    
    private readonly IAudioPlayer audioPlayer;

    public PlaybackVm(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
        
        Volume = audioPlayer.Volume;

        audioPlayer.PlaybackStarted += () =>
        {
            PlayingTrackViewVm ??= new TrackViewVm();
            
            // TODO The cover should probably NOT be re-created every time you switch a track
            PlayingTrackViewVm.Update(audioPlayer.NowPlaying!);
        };

        audioPlayer.PlaybackPaused += () => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
        });
        
        audioPlayer.PlaybackResumed += () => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = true;
        });

        audioPlayer.PlaybackFinished += () => Dispatcher.UIThread.Post(() =>
        {
            audioPlayer.PlayNextTrack();
        });
        
        audioPlayer.PlaybackPositionChanged += value => Dispatcher.UIThread.Post(() =>
        {
            if(!IsSeeking)
            {
                PlaybackPosition = value;
            }
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
    public void Pause()
    {
        audioPlayer.TogglePause();
    }

    [RelayCommand]
    public void PlayNextTrack()
    {
        audioPlayer.PlayNextTrack();
    }

    [RelayCommand]
    public void PlayPreviousTrack()
    {
        audioPlayer.PlayPreviousTrack();
    }
    
    partial void OnVolumeChanged(int value)
    {
        if(IsChangingVolume)
        {
            audioPlayer.Volume = Math.Clamp(value, 0, 100);
        }
    }

    partial void OnPlaybackPositionChanged(float value)
    {
        if(IsSeeking)
        {
            audioPlayer.PlaybackPosition = value;
        }
    }
}
