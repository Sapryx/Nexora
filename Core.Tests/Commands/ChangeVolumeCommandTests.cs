using Core.Commands;
using Core.Playback;
using Moq;

namespace Core.Tests.Commands;

public class ChangeVolumeCommandTests
{
    private readonly Mock<IAudioEngine> audioEngineMock;
    private readonly ChangeVolumeCommand command;

    public ChangeVolumeCommandTests()
    {
        audioEngineMock = new Mock<IAudioEngine>();
        command = new ChangeVolumeCommand(audioEngineMock.Object);
    }

    [Fact]
    public void Execute_DelegatesToAudioEngine()
    {
        command.Execute(4);
        audioEngineMock.VerifySet(it => it.Volume = 4, Times.Once);
    }
    
    [Fact]
    public void Execute_NegativeVolume_ClipsToZero_DelegatesToAudioEngine()
    {
        command.Execute(-10);
        audioEngineMock.VerifySet(it => it.Volume = 0, Times.Once);
    }
}
