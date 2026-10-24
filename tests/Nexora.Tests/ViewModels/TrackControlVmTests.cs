using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class TrackControlVmTests
{
    private readonly PlaylistRegistry playlistRegistry = new PlaylistRegistry();
    private readonly Mock<IAudioPlayer> audioPlayerMock = new Mock<IAudioPlayer>();
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly TrackMetadataEditor metadataEditor = new TrackMetadataEditor(
        NullLogger<TrackMetadataEditor>.Instance,
        Mock.Of<IMetadataSaver>(),
        Mock.Of<IPendingMetadataStorage>(),
        Mock.Of<IAudioPlayer>()
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

    [Fact]
    public void Constructor_QueueItem_IsInQueue()
    {
        playbackQueue.AddTrack(CreateTrack());

        var vm = CreateVm(playbackQueue.GetItem(0));

        Assert.True(vm.IsInQueue);
    }

    [Fact]
    public void Constructor_GlobalPlaylistItem_IsNotInQueue()
    {
        playlistRegistry.GlobalPlaylist.AddTrack(CreateTrack());

        var vm = CreateVm(playlistRegistry.GlobalPlaylist.GetItem(0));

        Assert.False(vm.IsInQueue);
    }

    [Fact]
    public void AddToQueue_NonEmptyQueue_AddsTrackToEnd()
    {
        var queued = CreateTrack();
        var track = CreateTrack();
        playbackQueue.AddTrack(queued);
        playlistRegistry.GlobalPlaylist.AddTrack(track);
        var vm = CreateVm(playlistRegistry.GlobalPlaylist.GetItem(0));

        vm.AddToQueue();

        Assert.Equal([queued, track], playbackQueue.Select(it => it.AudioTrack));
    }

    [Fact]
    public void AddToQueue_CalledTwice_AddsTrackTwice()
    {
        var track = CreateTrack();
        playlistRegistry.GlobalPlaylist.AddTrack(track);
        var vm = CreateVm(playlistRegistry.GlobalPlaylist.GetItem(0));

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
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(item);
        var vm = CreateVm(item);

        vm.RemoveFromQueue();

        audioPlayerMock.Verify(it => it.PlayTrack(next));
    }

    [Fact]
    public void PlaybackStopped_ActiveTrack_MarksTrackInactive()
    {
        var playlistItem = CreatePlaylistItem(TimeSpan.Zero);
        var vm = CreateVm(playlistItem);
        audioPlayerMock.Raise(it => it.PlaybackStarted += null, playlistItem);

        audioPlayerMock.Raise(it => it.PlaybackStopped += null);

        Assert.False(vm.IsActive);
        Assert.False(vm.IsActiveAndPlaying);
    }

    private static IAudioTrack CreateTrack()
    {
        return Mock.Of<IAudioTrack>(it => it.Metadata == new Metadata() && it.AudioPath == "");
    }

    [Fact]
    public void BeginEdit_TrackShown_CopiesTitleAndArtistsToEditedFields()
    {
        var vm = CreateVm(CreatePlaylistItem("Psycho", "Muse"));

        vm.BeginEdit();

        Assert.Equal("Psycho", vm.EditedTitle);
        Assert.Equal("Muse", vm.EditedArtists);
    }

    [Fact]
    public async Task ApplyEditAsync_FieldsEdited_ShowsNewTitleAndArtists()
    {
        var vm = CreateVm(CreatePlaylistItem("Psycho", "Muse"));
        vm.BeginEdit();
        vm.EditedTitle = "Uprising";
        vm.EditedArtists = "Muse, Queen";

        await vm.ApplyEditAsync();

        Assert.Equal("Uprising", vm.Title);
        Assert.Equal("Muse, Queen", vm.Artists);
    }

    [Fact]
    public async Task MetadataChanged_SameTrackInOtherVm_ShowsNewTitle()
    {
        var playlistItem = CreatePlaylistItem("Psycho", "Muse");
        var editedVm = CreateVm(playlistItem);
        var otherVm = CreateVm(playlistItem);
        editedVm.BeginEdit();
        editedVm.EditedTitle = "Uprising";

        await editedVm.ApplyEditAsync();

        Assert.Equal("Uprising", otherVm.Title);
    }

    [Fact]
    public async Task MetadataChanged_OtherTrack_KeepsTitle()
    {
        var editedVm = CreateVm(CreatePlaylistItem("Psycho", "Muse"));
        var otherVm = CreateVm(CreatePlaylistItem("Madness", "Muse"));
        editedVm.BeginEdit();
        editedVm.EditedTitle = "Uprising";

        await editedVm.ApplyEditAsync();

        Assert.Equal("Madness", otherVm.Title);
    }

    [Fact]
    public async Task ApplyEditAsync_TrackPlaying_ShowsUnsavedChangesUntilSaved()
    {
        var playlistItem = CreatePlaylistItem("Psycho", "Muse");
        var playingAudioPlayerMock = new Mock<IAudioPlayer>();
        playingAudioPlayerMock.SetupGet(it => it.NowPlaying).Returns(playlistItem);
        playingAudioPlayerMock
            .Setup(it => it.ReleaseFileAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());
        var editor = new TrackMetadataEditor(
            NullLogger<TrackMetadataEditor>.Instance,
            Mock.Of<IMetadataSaver>(),
            Mock.Of<IPendingMetadataStorage>(),
            playingAudioPlayerMock.Object
        );
        var vm = CreateVm(playlistItem, editor);
        vm.BeginEdit();
        vm.EditedTitle = "Uprising";

        await vm.ApplyEditAsync();
        bool hadUnsavedChanges = vm.HasUnsavedChanges;
        await editor.SaveAllAsync();

        Assert.True(hadUnsavedChanges);
        Assert.False(vm.HasUnsavedChanges);
        Assert.Equal("Uprising", vm.Title);
    }

    private static IPlaylistItem CreatePlaylistItem(string title, string artists)
    {
        var metadata = new Metadata() { Title = title, Artists = artists };
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
        return CreateVm(playlistItem, metadataEditor);
    }

    private TrackControlVm CreateVm(IPlaylistItem playlistItem, TrackMetadataEditor editor)
    {
        return new TrackControlVm(
            playlistItem,
            playbackQueue,
            audioPlayerMock.Object,
            editor,
            new TrackEditorVm(),
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);
    }
}
