using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class TrackEditorVmTests
{
    private readonly TrackEditorVm vm = new TrackEditorVm();
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly TrackMetadataEditor metadataEditor = new TrackMetadataEditor(
        NullLogger<TrackMetadataEditor>.Instance,
        Mock.Of<IMetadataSaver>(),
        Mock.Of<IPendingMetadataStorage>(),
        Mock.Of<IAudioPlayer>()
    );

    public TrackEditorVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());
    }

    [Fact]
    public void EditCommand_TrackExecutes_OpensTrackWithFieldsCopied()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");

        trackVm.EditCommand.Execute(null);

        Assert.Same(trackVm, vm.TrackVm);
        Assert.Equal("Psycho", trackVm.EditedTitle);
        Assert.Equal("Muse", trackVm.EditedArtists);
    }

    [Fact]
    public void Open_OtherTrackWasEdited_AppliesItsEdit()
    {
        var editedVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(editedVm);
        editedVm.EditedTitle = "Uprising";

        vm.Open(CreateTrackVm("Madness", "Muse"));

        Assert.Equal("Uprising", editedVm.Title);
    }

    [Fact]
    public void Open_SameTrackAgain_KeepsEditedFields()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        trackVm.EditedTitle = "Uprising";

        vm.Open(trackVm);

        Assert.Equal("Uprising", trackVm.EditedTitle);
        Assert.Equal("Psycho", trackVm.Title);
    }

    [Fact]
    public void Close_TrackWasEdited_AppliesEditAndClears()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        trackVm.EditedArtists = "Muse, Queen";

        vm.CloseCommand.Execute(null);

        Assert.Null(vm.TrackVm);
        Assert.Equal("Muse, Queen", trackVm.Artists);
    }

    [Fact]
    public async Task ApplyAsync_NoTrackOpen_Completes()
    {
        await vm.ApplyAsync();

        Assert.Null(vm.TrackVm);
    }

    private TrackControlVm CreateTrackVm(string title, string artists)
    {
        var metadata = new Metadata() { Title = title, Artists = artists };
        var playlistItem = Mock.Of<IPlaylistItem>(
            it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == $"{title}.mp3"
        );

        return new TrackControlVm(
            playlistItem,
            new PlaybackQueue(Mock.Of<IAudioPlayer>()),
            Mock.Of<IAudioPlayer>(),
            metadataEditor,
            vm,
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);
    }
}
