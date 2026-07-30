using Core.Commands;
using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Commands;

public class ToggleTrackCommandTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock;
    private readonly Mock<IPlayTrackCommand> playTrackCommandMock;
    private readonly Mock<IPauseTrackCommand> pauseTrackCommandMock;
    private readonly ToggleTrackCommand toggleTrackCommand;

    public ToggleTrackCommandTests()
    {
        audioPlayerMock = new Mock<IAudioPlayer>();
        playTrackCommandMock = new Mock<IPlayTrackCommand>();
        pauseTrackCommandMock = new Mock<IPauseTrackCommand>();
        toggleTrackCommand = new ToggleTrackCommand(
            audioPlayerMock.Object, 
            playTrackCommandMock.Object, 
            pauseTrackCommandMock.Object
        );
    }
    
    [Fact]
    public void Execute_TrackAlreadyPlaying_CallsPauseTrackCommand()
    {
        var playlistItemMock = new Mock<IPlaylistItem>();
        
        audioPlayerMock
            .SetupGet(it => it.NowPlaying)
            .Returns(playlistItemMock.Object);
        
        toggleTrackCommand.Execute(playlistItemMock.Object);
        
        pauseTrackCommandMock.Verify(it => it.Execute(), Times.Once);
    }
    
    [Fact]
    public void Execute_SomethingElseIsPlaying_CallsPlayTrackCommand()
    {
        var playlistItemMock = new Mock<IPlaylistItem>();
        var anotherPlaylistItemMock = new Mock<IPlaylistItem>();
        
        audioPlayerMock
            .SetupGet(it => it.NowPlaying)
            .Returns(anotherPlaylistItemMock.Object);
        
        toggleTrackCommand.Execute(playlistItemMock.Object);
        
        playTrackCommandMock.Verify(it => it.Execute(playlistItemMock.Object), Times.Once);
    }
}
