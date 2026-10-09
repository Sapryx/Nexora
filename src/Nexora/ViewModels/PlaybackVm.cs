using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Playback;
using Nexora.Media;

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
    
    [ObservableProperty]
    public partial bool IsMuted { get; private set; }

    public bool IsDraggingVolume { get; set; }
    public bool IsDraggingPosition { get; set; }
    
    private readonly IAudioPlayer audioPlayer;
    private int volumeBeforeMute;
    private bool isUpdatingDisplay;

    public PlaybackVm(IAudioPlayer audioPlayer, ICoverCache coverCache)
    {
        this.audioPlayer = audioPlayer;
        
        SetVolumeDisplay(audioPlayer.Volume);

        audioPlayer.PlaybackStarted += () => Dispatcher.UIThread.Post(() =>
        {
            var trackViewVm = PlayingTrackViewVm ?? new TrackViewVm(coverCache);
            trackViewVm.Update(audioPlayer.NowPlaying!);
            PlayingTrackViewVm = trackViewVm;
        });

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
            if(!IsDraggingPosition)
            {
                SetPlaybackPositionDisplay(value);
            }
        });

        audioPlayer.VolumeChanged += newVolume => Dispatcher.UIThread.Post(() =>
        {
            int volume = (int)MathF.Round(newVolume);

            if(IsMuted)
            {
                volumeBeforeMute = volume;
            }
            else if(!IsDraggingVolume)
            {
                SetVolumeDisplay(volume);
            }
        });

        audioPlayer.MuteChanged += isMuted => Dispatcher.UIThread.Post(() =>
        {
            ApplyMuteState(isMuted);
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

    [RelayCommand]
    public void ToggleMute()
    {
        SetMuted(!IsMuted);
    }

    public void SkipForward()
    {
        audioPlayer.SkipForward();
    }

    public void SkipBack()
    {
        audioPlayer.SkipBack();
    }
    
    partial void OnVolumeChanged(int value)
    {
        if(isUpdatingDisplay)
        {
            return;
        }

        audioPlayer.Volume = Math.Clamp(value, 0, 100);

        if(IsMuted && value > 0)
        {
            volumeBeforeMute = value;
            SetMuted(false);
        }
    }

    private void SetMuted(bool isMuted)
    {
        audioPlayer.Mute = isMuted;
        ApplyMuteState(isMuted);
    }

    private void ApplyMuteState(bool isMuted)
    {
        if(IsMuted == isMuted)
        {
            return;
        }

        IsMuted = isMuted;

        if(isMuted)
        {
            volumeBeforeMute = Volume;
            SetVolumeDisplay(0);
        }
        else if(!IsDraggingVolume)
        {
            SetVolumeDisplay(volumeBeforeMute);
        }
    }

    partial void OnPlaybackPositionChanged(float value)
    {
        if(!isUpdatingDisplay)
        {
            audioPlayer.PlaybackPosition = value;
        }
    }

    private void SetVolumeDisplay(int volume)
    {
        isUpdatingDisplay = true;
        Volume = volume;
        isUpdatingDisplay = false;
    }

    private void SetPlaybackPositionDisplay(float position)
    {
        isUpdatingDisplay = true;
        PlaybackPosition = position;
        isUpdatingDisplay = false;
    }
}
