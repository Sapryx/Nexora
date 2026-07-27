using Core.Playback;
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
    public void Volume_Get_Set_DelegatedToAudioEngine()
    {
        _ = audioPlayer.Volume;
        audioPlayer.Volume = 13;
        
        audioEngineMock.VerifyGet(it => it.Volume, Times.Once);
        audioEngineMock.VerifySet(it => it.Volume = 13, Times.Once);
    }
}
