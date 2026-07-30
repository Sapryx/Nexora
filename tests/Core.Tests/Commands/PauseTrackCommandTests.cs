using Core.Commands;
using Core.Playback;
using Moq;

namespace Core.Tests.Commands;

public class PauseTrackCommandTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock;
    private readonly PauseTrackCommand command;

    public PauseTrackCommandTests()
    {
        audioPlayerMock = new Mock<IAudioPlayer>();
        command = new PauseTrackCommand(audioPlayerMock.Object);
    }
    
    [Fact]
    public void Execute_DelegatesToAudioPlayer()
    {
        command.Execute();
        audioPlayerMock.Verify(it => it.TogglePause(), Times.Once);
    }
}
