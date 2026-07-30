using Core.Commands;
using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Commands;

public class PlayPreviousTrackCommandTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock;
    private readonly Mock<IPlayTrackCommand> playTrackCommandMock;
    private readonly PlayPreviousTrackCommand playPreviousTrackCommand;

    public PlayPreviousTrackCommandTests()
    {
        audioPlayerMock = new Mock<IAudioPlayer>();
        playTrackCommandMock = new Mock<IPlayTrackCommand>();
        playPreviousTrackCommand = new PlayPreviousTrackCommand(audioPlayerMock.Object, playTrackCommandMock.Object);
    }

    [Fact]
    public void Execute_SomethingIsPlaying_CallsPlayTrackCommandWithNextTrack()
    {
        var currentPlaylistItemMock = new Mock<IPlaylistItem>();
        var previousPlaylistItemMock = new Mock<IPlaylistItem>();

        audioPlayerMock
            .SetupGet(it => it.NowPlaying)
            .Returns(currentPlaylistItemMock.Object);
        
        currentPlaylistItemMock
            .Setup(it => it.GetPrevious())
            .Returns(previousPlaylistItemMock.Object);
        
        playPreviousTrackCommand.Execute();
        
        playTrackCommandMock.Verify(it => it.Execute(previousPlaylistItemMock.Object));
    }   
}
