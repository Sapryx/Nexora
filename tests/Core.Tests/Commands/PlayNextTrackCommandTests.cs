using Core.Commands;
using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Commands;

public class PlayNextTrackCommandTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock;
    private readonly Mock<IPlayTrackCommand> playTrackCommandMock;
    private readonly PlayNextTrackCommand playNextTrackCommand;

    public PlayNextTrackCommandTests()
    {
        audioPlayerMock = new Mock<IAudioPlayer>();
        playTrackCommandMock = new Mock<IPlayTrackCommand>();
        playNextTrackCommand = new PlayNextTrackCommand(audioPlayerMock.Object, playTrackCommandMock.Object);
    }

    [Fact]
    public void Execute_SomethingIsPlaying_CallsPlayTrackCommandWithNextTrack()
    {
        var currentPlaylistItemMock = new Mock<IPlaylistItem>();
        var nextPlaylistItemMock = new Mock<IPlaylistItem>();

        audioPlayerMock
            .SetupGet(it => it.NowPlaying)
            .Returns(currentPlaylistItemMock.Object);
        
        currentPlaylistItemMock
            .Setup(it => it.GetNext())
            .Returns(nextPlaylistItemMock.Object);
        
        playNextTrackCommand.Execute();
        
        playTrackCommandMock.Verify(it => it.Execute(nextPlaylistItemMock.Object));
    }
}
