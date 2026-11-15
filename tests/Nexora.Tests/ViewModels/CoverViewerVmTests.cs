using Core.Library;
using Core.Playlists;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class CoverViewerVmTests
{
    private readonly Mock<ICoverCache> coverCacheMock = new Mock<ICoverCache>();
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly CoverViewerVm vm;

    public CoverViewerVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        vm = new CoverViewerVm(coverCacheMock.Object, uiDispatcherMock.Object);
    }

    [Fact]
    public void Open_Track_OpensAndLoadsOriginalCover()
    {
        vm.Open("Psycho.mp3", null);

        Assert.True(vm.IsOpen);
        coverCacheMock.Verify(it => it.LoadOriginalAsync("Psycho.mp3"), Times.Once);
    }

    [Fact]
    public void Close_Open_Closes()
    {
        vm.Open("Psycho.mp3", null);

        vm.Close();

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void ShowCover_TrackHasNoCover_DoesNotOpen()
    {
        var trackViewVm = new TrackViewVm(coverCacheMock.Object, vm, uiDispatcherMock.Object);
        var metadata = new Metadata() { Title = "Psycho", Artists = ["Muse"] };
        trackViewVm.Update(Mock.Of<IPlaylistItem>(
            it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == "Psycho.mp3"
        ));

        trackViewVm.ShowCover();

        Assert.False(vm.IsOpen);
    }
}
