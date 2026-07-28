using Core.Commands;
using Core.Playback;
using Moq;

namespace Core.Tests.Commands;

public class ChangeVolumeCommandTests
{
    private readonly Mock<IAudioPlayer> audioPlayer;
    private readonly ChangeVolumeCommand command;

    public ChangeVolumeCommandTests()
    {
        audioPlayer = new Mock<IAudioPlayer>();
        command = new ChangeVolumeCommand(audioPlayer.Object);
    }

    [Fact]
    public void Execute_DelegatesToAudioEngine()
    {
        command.Execute(4);
        audioPlayer.VerifySet(it => it.Volume = 4, Times.Once);
    }
    
    [Fact]
    public void Execute_NegativeVolume_ClipsToZero_DelegatesToAudioEngine()
    {
        command.Execute(-10);
        audioPlayer.VerifySet(it => it.Volume = 0, Times.Once);
    }
}
