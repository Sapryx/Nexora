using Core.Playback;

namespace Core.Tests;

public class AudioPlayerTests
{
    private readonly FakeAudioEngine audioEngine;
    private readonly AudioPlayer audioPlayer;
    
    public AudioPlayerTests()
    {
        audioEngine = new FakeAudioEngine();
        audioPlayer = new AudioPlayer(audioEngine);
    }
    
    [Fact]
    public void NewAudioPlayer_NowPlayingIsNull()
    {
        Assert.Null(audioPlayer.NowPlaying);
    }

    [Fact]
    public void NewAudioPlayer_IsNotPlaying()
    {
        Assert.False(audioPlayer.IsPlaying);
    }

    [Fact]
    public void GetAndSetVolume_DelegatesToAudioEngine()
    {
        Assert.Equal(audioEngine.Volume, audioPlayer.Volume);

        audioPlayer.Volume = 39;
        
        Assert.Equal(39, audioPlayer.Volume);
        Assert.Equal(39, audioEngine.Volume);
    }
}
