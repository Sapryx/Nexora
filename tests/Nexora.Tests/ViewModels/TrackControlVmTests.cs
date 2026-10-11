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
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();

    public TrackControlVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());
    }

    [Theory]
    [InlineData(232, "03:52")]
    [InlineData(209, "03:29")]
    [InlineData(59, "00:59")]
    [InlineData(600, "10:00")]
    public void Constructor_TrackHasDuration_ShowsWholeMinutesAndSeconds(int seconds, string expected)
    {
        var vm = CreateVm(CreatePlaylistItem(TimeSpan.FromSeconds(seconds)));

        Assert.Equal(expected, vm.Duration);
    }

    [Fact]
    public void PlaybackStarted_RaisedWithThisTrack_MarksTrackActiveAndPlaying()
    {
        var playlistItem = CreatePlaylistItem(TimeSpan.Zero);
        var vm = CreateVm(playlistItem);

        audioPlayerMock.Raise(it => it.PlaybackStarted += null, playlistItem);

        Assert.True(vm.IsActive);
        Assert.True(vm.IsActiveAndPlaying);
    }

    [Fact]
    public void PlaybackStarted_RaisedWithOtherTrack_MarksTrackInactive()
    {
        var playlistItem = CreatePlaylistItem(TimeSpan.Zero);
        var vm = CreateVm(playlistItem);
        audioPlayerMock.Raise(it => it.PlaybackStarted += null, playlistItem);

        audioPlayerMock.Raise(it => it.PlaybackStarted += null, CreatePlaylistItem(TimeSpan.Zero));

        Assert.False(vm.IsActive);
        Assert.False(vm.IsActiveAndPlaying);
    }

    private static IPlaylistItem CreatePlaylistItem(TimeSpan duration)
    {
        var metadata = new Metadata() { Duration = duration };
        return Mock.Of<IPlaylistItem>(
            it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == ""
        );
    }

    private TrackControlVm CreateVm(IPlaylistItem playlistItem)
    {
        return new TrackControlVm(
            playlistItem,
            audioPlayerMock.Object,
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);
    }
}
