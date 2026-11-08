using Core.Library;
using Core.Playlists;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Tests.Shared.Playback;

namespace Nexora.Tests.ViewModels;

public class QueueVmTests
{
    private readonly PlaybackQueue playbackQueue = new PlaybackQueue(AudioPlayerMock.Create().Object);
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly QueueVm vm;

    public QueueVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        var metadataEditor = new TrackMetadataEditor(
            NullLogger<TrackMetadataEditor>.Instance,
            Mock.Of<IMetadataSaver>(),
            AudioPlayerMock.Create().Object
        );
        var trackControlVmRegistry = new TrackControlVmRegistry(
            playbackQueue,
            AudioPlayerMock.Create().Object,
            metadataEditor,
            new TrackEditorVm(
                new ArtistRegistry(metadataEditor),
                AudioPlayerMock.Create().Object,
                Mock.Of<ICoverCache>(),
                uiDispatcherMock.Object
            ),
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);

        vm = new QueueVm(playbackQueue, trackControlVmRegistry, uiDispatcherMock.Object);
    }

    [Fact]
    public void Constructor_EmptyQueue_IsEmpty()
    {
        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.TrackVms);
    }

    [Fact]
    public void ItemAdded_SeveralTracks_AppendsTrackVmsInOrder()
    {
        playbackQueue.AddTracks([CreateTrack("Psycho"), CreateTrack("Complicated")]);

        Assert.Equal(["Psycho", "Complicated"], vm.TrackVms.Select(it => it.Title));
        Assert.False(vm.IsEmpty);
    }

    [Fact]
    public void ItemAdded_SameTrackTwice_AddsTwoTrackVms()
    {
        var track = CreateTrack("Psycho");

        playbackQueue.AddTracks([track, track]);

        Assert.Equal(["Psycho", "Psycho"], vm.TrackVms.Select(it => it.Title));
    }

    [Fact]
    public void ItemRemoved_MiddleItem_RemovesItsTrackVm()
    {
        playbackQueue.AddTracks([CreateTrack("Psycho"), CreateTrack("Aether"), CreateTrack("Apathy")]);

        playbackQueue.RemoveItem(playbackQueue.GetItem(1));

        Assert.Equal(["Psycho", "Apathy"], vm.TrackVms.Select(it => it.Title));
    }

    [Fact]
    public void ItemRemoved_LastRemainingItem_IsEmpty()
    {
        playbackQueue.AddTrack(CreateTrack("Psycho"));

        playbackQueue.RemoveItem(playbackQueue.GetItem(0));

        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.TrackVms);
    }

    private static IAudioTrack CreateTrack(string title)
    {
        return Mock.Of<IAudioTrack>(it =>
            it.Metadata == new Metadata() { Title = title } &&
            it.AudioPath == $"{title}.mp3");
    }
}
