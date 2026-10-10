using Core.Playback;
using Core.Playlists;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class TrackControlVmTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock = new Mock<IAudioPlayer>();

    [Theory]
    [InlineData(232, "03:52")]
    [InlineData(209, "03:29")]
    [InlineData(59, "00:59")]
    [InlineData(600, "10:00")]
    public void Constructor_TrackHasDuration_ShowsWholeMinutesAndSeconds(int seconds, string expected)
    {
        var vm = CreateVm(TimeSpan.FromSeconds(seconds));

        Assert.Equal(expected, vm.Duration);
    }

    private TrackControlVm CreateVm(TimeSpan duration)
    {
        var metadata = new Metadata() { Duration = duration };
        var playlistItem = Mock.Of<IPlaylistItem>(it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == "");

        return new TrackControlVm(
            playlistItem,
            audioPlayerMock.Object,
            Mock.Of<ICoverCache>(),
            Mock.Of<IUiDispatcher>());
    }
}
