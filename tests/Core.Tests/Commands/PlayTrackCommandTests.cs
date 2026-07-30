using Core.Commands;
using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Commands;

public class PlayTrackCommandTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock;
    private readonly PlayTrackCommand command;

    public PlayTrackCommandTests()
    {
        audioPlayerMock = new Mock<IAudioPlayer>();
        command = new PlayTrackCommand(audioPlayerMock.Object);
    }

    [Fact]
    public void Execute_DelegatesToAudioPlayer()
    {
        var playlistItemMock = new Mock<IPlaylistItem>();
        command.Execute(playlistItemMock.Object);
        
        audioPlayerMock.Verify(it => it.PlayTrack(playlistItemMock.Object), Times.Once);
    }
}
