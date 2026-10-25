using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Shared.Logging;

namespace Core.Tests.Storage;

public class TrackMetadataEditorTests
{
    private const string AudioPath = "/music/Psycho.mp3";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);
    private readonly Mock<IMetadataSaver> metadataSaverMock = new Mock<IMetadataSaver>();
    private readonly Mock<IPendingMetadataStorage> pendingStorageMock = new Mock<IPendingMetadataStorage>();
    private readonly Mock<IAudioPlayer> audioPlayerMock = new Mock<IAudioPlayer>();
    private readonly TestLogger<TrackMetadataEditor> logger = new TestLogger<TrackMetadataEditor>();
    private readonly AudioTrack audioTrack = new AudioTrack(
        AudioPath,
        new Metadata() { Title = "Psycho", Artists = ["Muse"] }
    );
    private readonly List<string> changedPaths = [];
    private readonly TrackMetadataEditor editor;

    public TrackMetadataEditorTests()
    {
        pendingStorageMock
            .Setup(it => it.LoadAll())
            .Returns([]);

        audioPlayerMock
            .Setup(it => it.ReleaseFileAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

        editor = new TrackMetadataEditor(
            logger,
            metadataSaverMock.Object,
            pendingStorageMock.Object,
            audioPlayerMock.Object
        );
        editor.MetadataChanged += it =>
        {
            lock(changedPaths)
            {
                changedPaths.Add(it);
            }
        };
    }

    [Fact]
    public async Task EditAsync_NothingChanged_DoesNotSave()
    {
        await editor.EditAsync(audioTrack, " Psycho ", ["Muse"]);

        metadataSaverMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
        pendingStorageMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
        Assert.Empty(changedPaths);
    }

    [Fact]
    public async Task EditAsync_TitleChanged_SavesOnlyTitleAndDeletesPendingFile()
    {
        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change =>
            change.AudioPath == AudioPath && change.Title == "Uprising" && change.Artists == null)));
        pendingStorageMock.Verify(it => it.Delete(AudioPath));
        Assert.Equal("Uprising", audioTrack.Metadata.Title);
        Assert.False(editor.HasUnsavedChanges(AudioPath));
        Assert.Equal([AudioPath, AudioPath], changedPaths);
    }

    [Fact]
    public async Task EditAsync_ArtistsChanged_SavesNormalizedArtistsOnly()
    {
        await editor.EditAsync(audioTrack, "Psycho", [" Muse", "Queen ", "", " ", "muse"]);

        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change =>
            change.Title == null && change.Artists != null)));
        Assert.Equal(["Muse", "Queen"], audioTrack.Metadata.Artists);
    }

    [Fact]
    public async Task EditAsync_TitleCleared_SavesEmptyTitleAndShowsFileName()
    {
        audioTrack.Metadata.Title = "Uprising";

        await editor.EditAsync(audioTrack, "  ", ["Muse"]);

        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change => change.Title == "")));
        Assert.Equal("Psycho", audioTrack.Metadata.Title);
    }

    [Fact]
    public async Task EditAsync_TitleClearedAndAlreadyFileName_DoesNotSave()
    {
        await editor.EditAsync(audioTrack, "", ["Muse"]);

        metadataSaverMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
    }

    [Fact]
    public async Task EditAsync_SaveFails_LogsErrorAndKeepsChangePending()
    {
        var exception = new IOException("File is locked");
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Throws(exception);

        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.Equal("Uprising", audioTrack.Metadata.Title);
        Assert.True(editor.HasUnsavedChanges(AudioPath));
        pendingStorageMock.Verify(it => it.Delete(It.IsAny<string>()), Times.Never);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(AudioPath, entry.Message);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public async Task EditAsync_TrackPlaying_ShowsChangeAndKeepsItPending()
    {
        SetupNowPlaying(audioTrack);

        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.Equal("Uprising", audioTrack.Metadata.Title);
        Assert.True(editor.HasUnsavedChanges(AudioPath));
        pendingStorageMock.Verify(it => it.Save(It.Is<MetadataChange>(change => change.Title == "Uprising")));
        metadataSaverMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
    }

    [Fact]
    public async Task EditAsync_TrackPlayingEditedTwice_MergesPendingChanges()
    {
        SetupNowPlaying(audioTrack);

        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);
        await editor.EditAsync(audioTrack, "Uprising", ["Queen"]);

        pendingStorageMock.Verify(it => it.Save(It.Is<MetadataChange>(change =>
            change.Title == "Uprising" && change.Artists != null && change.Artists[0] == "Queen")));
    }

    [Fact]
    public async Task PlaybackStarted_OtherTrack_SavesPendingChange()
    {
        SetupNowPlaying(audioTrack);
        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);
        var otherItem = CreatePlaylistItem(new AudioTrack("/music/Madness.mp3", new Metadata()));
        SetupNowPlaying(otherItem.AudioTrack);

        audioPlayerMock.Raise(it => it.PlaybackStarted += null, otherItem);

        await WaitUntilSavedAsync();
        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change => change.Title == "Uprising")));
        pendingStorageMock.Verify(it => it.Delete(AudioPath));
    }

    [Fact]
    public async Task PlaybackPaused_PlayingTrackHasChanges_SavesWithFileReleased()
    {
        SetupNowPlaying(audioTrack);
        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        audioPlayerMock.Raise(it => it.PlaybackPaused += null);

        await WaitUntilSavedAsync();
        audioPlayerMock.Verify(it => it.ReleaseFileAsync(It.IsAny<Func<Task>>()));
        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change => change.Title == "Uprising")));
    }

    [Fact]
    public async Task PlaybackPaused_NoChanges_DoesNotReleaseFile()
    {
        SetupNowPlaying(audioTrack);

        audioPlayerMock.Raise(it => it.PlaybackPaused += null);
        await Task.Delay(100);

        audioPlayerMock.Verify(it => it.ReleaseFileAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task SaveAllAsync_PlayingTrackHasChanges_SavesWithFileReleased()
    {
        SetupNowPlaying(audioTrack);
        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        await editor.SaveAllAsync();

        audioPlayerMock.Verify(it => it.ReleaseFileAsync(It.IsAny<Func<Task>>()));
        Assert.False(editor.HasUnsavedChanges(AudioPath));
    }

    [Fact]
    public void RecoverPendingChanges_SaveSucceeds_DeletesPendingFile()
    {
        SetupStoredChange(new MetadataChange() { AudioPath = AudioPath, Title = "Uprising" });

        editor.RecoverPendingChanges();

        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change => change.Title == "Uprising")));
        pendingStorageMock.Verify(it => it.Delete(AudioPath));
        Assert.False(editor.HasUnsavedChanges(AudioPath));
    }

    [Fact]
    public void RecoverPendingChanges_FileMissing_DropsChange()
    {
        SetupStoredChange(new MetadataChange() { AudioPath = AudioPath, Title = "Uprising" });
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Throws(new FileNotFoundException());

        editor.RecoverPendingChanges();

        pendingStorageMock.Verify(it => it.Delete(AudioPath));
        Assert.False(editor.HasUnsavedChanges(AudioPath));
    }

    [Fact]
    public void RecoverPendingChanges_SaveFails_KeepsChangeThatApplyShowsOnTrack()
    {
        SetupStoredChange(new MetadataChange() { AudioPath = AudioPath, Artists = ["Queen"] });
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Throws(new IOException("File is locked"));

        editor.RecoverPendingChanges();
        editor.ApplyPendingChanges([audioTrack]);

        Assert.True(editor.HasUnsavedChanges(AudioPath));
        Assert.Equal("Psycho", audioTrack.Metadata.Title);
        Assert.Equal(["Queen"], audioTrack.Metadata.Artists);
        pendingStorageMock.Verify(it => it.Delete(It.IsAny<string>()), Times.Never);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    private void SetupNowPlaying(IAudioTrack track)
    {
        var playlistItem = CreatePlaylistItem(track);

        audioPlayerMock
            .SetupGet(it => it.NowPlaying)
            .Returns(playlistItem);
    }

    private void SetupStoredChange(MetadataChange change)
    {
        pendingStorageMock
            .Setup(it => it.LoadAll())
            .Returns([change]);
    }

    private async Task WaitUntilSavedAsync()
    {
        var deadline = DateTime.UtcNow + WaitTimeout;

        while(editor.HasUnsavedChanges(AudioPath) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.False(editor.HasUnsavedChanges(AudioPath));
    }

    private static IPlaylistItem CreatePlaylistItem(IAudioTrack track)
    {
        return Mock.Of<IPlaylistItem>(it => it.AudioTrack == track);
    }
}
