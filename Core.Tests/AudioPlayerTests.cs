using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests;

public class AudioPlayerTests
{
    private readonly Mock<IAudioEngine> audioEngineMock;
    private readonly AudioPlayer audioPlayer;
    
    public AudioPlayerTests()
    {
        audioEngineMock = new Mock<IAudioEngine>();
        audioPlayer = new AudioPlayer(audioEngineMock.Object);
    }
    
    [Fact]
    public void NewAudioPlayer_NowPlayingIsNull()
    {
        Assert.Null(audioPlayer.NowPlaying);
    }

    [Fact]
    public void IsPlaying_Get_DelegatesToAudioEngine()
    {
        _ = audioPlayer.IsPlaying;
        audioEngineMock.VerifyGet(it => it.IsPlaying, Times.Once);
    }

    [Fact]
    public void Volume_Get_Set_DelegatesToAudioEngine()
    {
        _ = audioPlayer.Volume;
        audioPlayer.Volume = 13;
        
        audioEngineMock.VerifyGet(it => it.Volume, Times.Once);
        audioEngineMock.VerifySet(it => it.Volume = 13, Times.Once);
    }

    [Fact]
    public void PlaybackPosition_DelegatesToAudioEngine()
    {
        _ = audioPlayer.PlaybackPosition;
        audioPlayer.PlaybackPosition = 0.4f;
        
        audioEngineMock.VerifyGet(it => it.PlaybackPosition, Times.Once);
        audioEngineMock.VerifySet(it => it.PlaybackPosition = 0.4f, Times.Once);
    }

    [Fact]
    public void PlaybackStarted_Add_Remove_DelegatesToAudioEngine()
    {
        var handler = () => { };

        audioPlayer.PlaybackStarted += handler;
        audioPlayer.PlaybackStarted -= handler;
        
        audioEngineMock.VerifyAdd(it => it.PlaybackStarted += handler, Times.Once);
        audioEngineMock.VerifyRemove(it => it.PlaybackStarted -= handler, Times.Once);
    }
    
    [Fact]
    public void PlaybackPositionChanged_Add_Remove_DelegatesToAudioEngine()
    {
        var handler = (float value) => { };

        audioPlayer.PlaybackPositionChanged += handler;
        audioPlayer.PlaybackPositionChanged -= handler;
        
        audioEngineMock.VerifyAdd(it => it.PlaybackPositionChanged += handler, Times.Once);
        audioEngineMock.VerifyRemove(it => it.PlaybackPositionChanged -= handler, Times.Once);
    }
    
    [Fact]
    public void PlaybackPaused_Add_Remove_DelegatesToAudioEngine()
    {
        var handler = () => { };

        audioPlayer.PlaybackPaused += handler;
        audioPlayer.PlaybackPaused -= handler;
        
        audioEngineMock.VerifyAdd(it => it.PlaybackPaused += handler, Times.Once);
        audioEngineMock.VerifyRemove(it => it.PlaybackPaused -= handler, Times.Once);
    }

    [Fact]
    public void PlaybackFinished_Add_Remove_DelegatesToAudioEngine()
    {
        var handler = () => { };

        audioPlayer.PlaybackFinished += handler;
        audioPlayer.PlaybackFinished -= handler;
        
        audioEngineMock.VerifyAdd(it => it.PlaybackFinished += handler, Times.Once);
        audioEngineMock.VerifyRemove(it => it.PlaybackFinished -= handler, Times.Once);
    }
    
    [Fact]
    public void PlaybackResumed_Add_Remove_DelegatesToAudioEngine()
    {
        var handler = () => { };

        audioPlayer.PlaybackResumed += handler;
        audioPlayer.PlaybackResumed -= handler;
        
        audioEngineMock.VerifyAdd(it => it.PlaybackResumed += handler, Times.Once);
        audioEngineMock.VerifyRemove(it => it.PlaybackResumed -= handler, Times.Once);
    }
    
    [Fact]
    public void PlayTrack_SetsNowPlaying()
    {
        var playlistItem = new Mock<IPlaylistItem>();
        
        audioPlayer.PlayTrack(playlistItem.Object);
        
        Assert.Equal(playlistItem.Object, audioPlayer.NowPlaying);
    }

    [Fact]
    public void PlayTrack_DelegatesToAudioEngine()
    {
        var playlistItem = new Mock<IPlaylistItem>();
        
        audioPlayer.PlayTrack(playlistItem.Object);
        
        audioEngineMock.Verify(it => it.StartPlayback(playlistItem.Object.AudioTrack), Times.Once);
    }

    [Fact]
    public void TogglePause_DelegatesToAudioEngine()
    {
        var playlistItem = new Mock<IPlaylistItem>();
        
        audioPlayer.PlayTrack(playlistItem.Object);
        audioPlayer.TogglePause();
        
        audioEngineMock.Verify(it => it.TogglePause(), Times.Once);
    }

    [Fact]
    public void TogglePause_NothingIsPlaying_NothingHappens()
    {
        audioPlayer.TogglePause();
        
        audioEngineMock.Verify(it => it.TogglePause(), Times.Never);
    }
}
