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

public class TrackControlVmRegistryTests
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
    private readonly TrackControlVmRegistry registry;

    public TrackControlVmRegistryTests()
    {
        playbackQueue = new PlaybackQueue(audioPlayerMock.Object);

        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        registry = new TrackControlVmRegistry(
            playbackQueue,
            audioPlayerMock.Object,
            tagWriter,
            new TrackEditorVm(
                TestMusicLibrary.Create(tagWriter),
                tagWriter,
                AudioPlayerMock.Create().Object,
                Mock.Of<ICoverCache>(),
                uiDispatcherMock.Object
            ),
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);
    }

    [Fact]
    public void PlaybackPositionChanged_ManyTracks_PostsOnce()
    {
        playbackQueue.AddTracks([CreateTrack("Psycho"), CreateTrack("Aether"), CreateTrack("Apathy")]);

        foreach(var playlistItem in playbackQueue)
        {
            registry.Create(playlistItem);
        }

        uiDispatcherMock.Invocations.Clear();

        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.5f);

        uiDispatcherMock.Verify(it => it.Post(It.IsAny<Action>()), Times.Once());
    }

    [Fact]
    public void StateChanged_ManyTracks_PostsOnce()
    {
        var vms = new[] { CreateQueueVm(), CreateQueueVm(), CreateLibraryVm() };
        uiDispatcherMock.Invocations.Clear();

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(playbackQueue.GetItem(1)));

        uiDispatcherMock.Verify(it => it.Post(It.IsAny<Action>()), Times.Once());
        Assert.Equal([false, true, false], vms.Select(it => it.IsActive));
    }

    [Fact]
    public void StateChanged_PlayingThisTrack_MarksTrackActiveAndPlaying()
    {
        var vm = CreateLibraryVm();

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(LibraryItem(0)));

        Assert.True(vm.IsActive);
        Assert.True(vm.IsActiveAndPlaying);
    }

    [Fact]
    public void StateChanged_PlayingOtherTrack_MarksTrackInactive()
    {
        var vm = CreateLibraryVm();
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(LibraryItem(0)));

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(CreateLibraryItem("Aether")));

        Assert.False(vm.IsActive);
        Assert.False(vm.IsActiveAndPlaying);
    }

    [Fact]
    public void StateChanged_PausedThisTrack_MarksTrackActiveButNotPlaying()
    {
        var vm = CreateLibraryVm();

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Paused(LibraryItem(0)));

        Assert.True(vm.IsActive);
        Assert.False(vm.IsActiveAndPlaying);
    }

    [Fact]
    public void StateChanged_StoppedActiveTrack_MarksTrackInactive()
    {
        var vm = CreateLibraryVm();
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(LibraryItem(0)));

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Stopped);

        Assert.False(vm.IsActive);
        Assert.False(vm.IsActiveAndPlaying);
    }

    [Fact]
    public void Create_TrackAlreadyPlaying_MarksTrackActive()
    {
        var playlistItem = CreateLibraryItem("Psycho");
        audioPlayerMock.SetupGet(it => it.State).Returns(PlaybackState.Playing(playlistItem));
        var vm = registry.Create(playlistItem);

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Stopped);

        Assert.False(vm.IsActive);
    }

    [Fact]
    public void PlaybackPositionChanged_ActiveQueueItem_ShowsProgress()
    {
        var vm = CreateQueueVm();
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(playbackQueue.GetItem(0)));

        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.5f);

        Assert.Equal(0.5, vm.Progress);
    }

    [Fact]
    public void PlaybackPositionChanged_ActiveLibraryPlaylistItem_KeepsZeroProgress()
    {
        var vm = CreateLibraryVm();
        audioPlayerMock.Raise(
            it => it.StateChanged += null,
            PlaybackState.Playing(LibraryItem(0))
        );

        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.5f);

        Assert.Equal(0, vm.Progress);
    }

    [Fact]
    public void StateChanged_ActiveQueueItemResumed_KeepsProgress()
    {
        var vm = CreateQueueVm();
        var playlistItem = playbackQueue.GetItem(0);
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(playlistItem));
        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.4f);
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Paused(playlistItem));

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(playlistItem));

        Assert.Equal(0.4f, vm.Progress);
        Assert.True(vm.IsActiveAndPlaying);
    }

    [Fact]
    public void StateChanged_PlayingOtherTrack_ResetsProgress()
    {
        var vm = CreateQueueVm();
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(playbackQueue.GetItem(0)));
        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.5f);

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(CreateLibraryItem("Aether")));

        Assert.Equal(0, vm.Progress);
    }

    [Fact]
    public async Task TrackChanged_TrackInLibraryAndQueue_PostsOnceAndShowsNewTitleInBoth()
    {
        var libraryVm = CreateLibraryVm();
        playbackQueue.AddTrack(LibraryItem(0).AudioTrack);
        var queueVm = registry.Create(playbackQueue.GetItem(0));
        uiDispatcherMock.Invocations.Clear();

        await tagWriter.EditAsync(LibraryItem(0).AudioTrack, "Uprising", ["Muse"]);

        uiDispatcherMock.Verify(it => it.Post(It.IsAny<Action>()), Times.Once());
        Assert.Equal(["Uprising", "Uprising"], new[] { libraryVm.Title, queueVm.Title });
    }

    [Fact]
    public async Task TrackChanged_OtherTrack_KeepsTitle()
    {
        var vm = CreateLibraryVm();

        await tagWriter.EditAsync(CreateLibraryItem("Aether").AudioTrack, "Uprising", ["Muse"]);

        Assert.Equal("Psycho", vm.Title);
    }

    [Fact]
    public async Task ItemRemoved_QueueItem_StopsUpdatingItsVm()
    {
        var vm = CreateQueueVm();
        var playlistItem = playbackQueue.GetItem(0);
        playbackQueue.RemoveItem(playlistItem);

        await tagWriter.EditAsync(playlistItem.AudioTrack, "Uprising", ["Muse"]);

        Assert.Equal("Psycho", vm.Title);
    }

    private TrackControlVm CreateQueueVm()
    {
        playbackQueue.AddTrack(CreateTrack("Psycho"));

        return registry.Create(playbackQueue.GetItem(playbackQueue.TrackCount - 1));
    }

    private TrackControlVm CreateLibraryVm()
    {
        return registry.Create(CreateLibraryItem("Psycho"));
    }

    private IPlaylistItem LibraryItem(int index)
    {
        return libraryPlaylist.GetItem(index);
    }

    private IPlaylistItem CreateLibraryItem(string title)
    {
        var playlist = libraryPlaylist;
        playlist.AddTrack(CreateTrack(title));

        return playlist.GetItem(playlist.TrackCount - 1);
    }

    private static IAudioTrack CreateTrack(string title)
    {
        return Mock.Of<IAudioTrack>(it =>
            it.Metadata == new Metadata() { Title = title } &&
            it.AudioPath == $"{title}.mp3");
    }
}
