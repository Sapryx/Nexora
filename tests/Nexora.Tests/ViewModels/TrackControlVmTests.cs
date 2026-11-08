using Core.Library;
using Core.Playback;
using Core.Playlists;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Tests.Shared.Library;
using Tests.Shared.Playback;

namespace Nexora.Tests.ViewModels;

public class TrackControlVmTests
{
    private readonly Playlist libraryPlaylist = new Playlist();
    private readonly Mock<IAudioPlayer> audioPlayerMock = AudioPlayerMock.Create();
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly TrackTagWriter tagWriter = new TrackTagWriter(
        NullLogger<TrackTagWriter>.Instance,
        Mock.Of<IMetadataSaver>(),
        AudioPlayerMock.Create().Object
    );
    private readonly PlaybackQueue playbackQueue;

    public TrackControlVmTests()
    {
        playbackQueue = new PlaybackQueue(audioPlayerMock.Object);

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
    public void Constructor_QueueItem_IsInQueue()
    {
        playbackQueue.AddTrack(CreateTrack());

        var vm = CreateVm(playbackQueue.GetItem(0));

        Assert.True(vm.IsInQueue);
    }

    [Fact]
    public void Constructor_LibraryPlaylistItem_IsNotInQueue()
    {
        libraryPlaylist.AddTrack(CreateTrack());

        var vm = CreateVm(libraryPlaylist.GetItem(0));

        Assert.False(vm.IsInQueue);
    }

    [Fact]
    public void AddToQueue_NonEmptyQueue_AddsTrackToEnd()
    {
        var queued = CreateTrack();
        var track = CreateTrack();
        playbackQueue.AddTrack(queued);
        libraryPlaylist.AddTrack(track);
        var vm = CreateVm(libraryPlaylist.GetItem(0));

        vm.AddToQueue();

        Assert.Equal([queued, track], playbackQueue.Select(it => it.AudioTrack));
    }

    [Fact]
    public void AddToQueue_CalledTwice_AddsTrackTwice()
    {
        var track = CreateTrack();
        libraryPlaylist.AddTrack(track);
        var vm = CreateVm(libraryPlaylist.GetItem(0));

        vm.AddToQueue();
        vm.AddToQueue();

        Assert.Equal([track, track], playbackQueue.Select(it => it.AudioTrack));
    }

    [Fact]
    public void RemoveFromQueue_QueueItem_RemovesOnlyThatItem()
    {
        var track = CreateTrack();
        playbackQueue.AddTrack(track);
        playbackQueue.AddTrack(track);
        var item = playbackQueue.GetItem(0);
        var vm = CreateVm(item);

        vm.RemoveFromQueue();

        Assert.Equal([track], playbackQueue.Select(it => it.AudioTrack));
        Assert.DoesNotContain(item, playbackQueue);
    }

    [Fact]
    public void RemoveFromQueue_PlayingItem_PlaysNextItem()
    {
        playbackQueue.AddTrack(CreateTrack());
        playbackQueue.AddTrack(CreateTrack());
        var item = playbackQueue.GetItem(0);
        var next = playbackQueue.GetItem(1);
        audioPlayerMock.SetupGet(it => it.State).Returns(PlaybackState.Playing(item));
        var vm = CreateVm(item);

        vm.RemoveFromQueue();

        audioPlayerMock.Verify(it => it.PlayTrack(next));
    }

    private static IAudioTrack CreateTrack()
    {
        return Mock.Of<IAudioTrack>(it => it.Metadata == new Metadata() && it.AudioPath == "");
    }

    [Fact]
    public async Task ApplyEditAsync_FieldsEdited_ShowsNewTitleAndArtists()
    {
        var vm = CreateVm(CreatePlaylistItem("Psycho", "Muse"));

        await vm.ApplyEditAsync("Uprising", ["Muse", "Queen"]);

        Assert.Equal("Uprising", vm.Title);
        Assert.Equal("Muse, Queen", vm.Artists);
    }

    private static IPlaylistItem CreatePlaylistItem(string title, string artists)
    {
        var metadata = new Metadata() { Title = title, Artists = [artists] };
        return Mock.Of<IPlaylistItem>(
            it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == $"{title}.mp3"
        );
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
        var registry = new TrackControlVmRegistry(
            playbackQueue,
            audioPlayerMock.Object,
            tagWriter,
            new TrackEditorVm(
                TestMusicLibrary.Create(tagWriter),
                audioPlayerMock.Object,
                Mock.Of<ICoverCache>(),
                uiDispatcherMock.Object
            ),
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);

        return registry.Create(playlistItem);
    }
}
