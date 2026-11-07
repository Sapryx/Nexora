using Core.Playback;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Shared.Logging;
using Tests.Shared.Playback;

namespace Core.Tests.Storage.Tags;

public class TrackMetadataEditorTests
{
    private const string AudioPath = "/music/Psycho.mp3";
    private readonly Mock<IMetadataSaver> metadataSaverMock = new Mock<IMetadataSaver>();
    private readonly Mock<IAudioPlayer> audioPlayerMock = AudioPlayerMock.Create();
    private readonly TestLogger<TrackMetadataEditor> logger = new TestLogger<TrackMetadataEditor>();
    private readonly AudioTrack audioTrack = new AudioTrack(
        AudioPath,
        new Metadata() { Title = "Psycho", Artists = ["Muse"] }
    );
    private readonly List<string> changedPaths = [];
    private readonly TrackMetadataEditor editor;

    public TrackMetadataEditorTests()
    {
        editor = new TrackMetadataEditor(logger, metadataSaverMock.Object, audioPlayerMock.Object);
        editor.MetadataChanged += it => changedPaths.Add(it);
    }

    [Fact]
    public async Task EditAsync_NothingChanged_DoesNotSave()
    {
        bool isSaved = await editor.EditAsync(audioTrack, " Psycho ", ["Muse"]);

        Assert.True(isSaved);
        metadataSaverMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
        Assert.Empty(changedPaths);
    }

    [Fact]
    public async Task EditAsync_TitleChanged_SavesOnlyTitleAndShowsIt()
    {
        bool isSaved = await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.True(isSaved);
        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change =>
            change.AudioPath == AudioPath && change.Title == "Uprising" && change.Artists == null)));
        Assert.Equal("Uprising", audioTrack.Metadata.Title);
        Assert.Equal([AudioPath], changedPaths);
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
    public async Task EditAsync_SaveFails_LogsErrorAndKeepsMetadata()
    {
        var exception = new IOException("File is locked");
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Throws(exception);

        bool isSaved = await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.False(isSaved);
        Assert.Equal("Psycho", audioTrack.Metadata.Title);
        Assert.Empty(changedPaths);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(AudioPath, entry.Message);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public async Task EditAsync_TitleChanged_SavesWhileTrackFileIsReleased()
    {
        bool wasReleasedDuringSave = false;
        bool isReleasing = false;
        audioPlayerMock
            .Setup(it => it.ReleaseFileAsync(AudioPath, It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>(async (audioPath, action) =>
            {
                isReleasing = true;
                await action();
                isReleasing = false;
            });
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Callback(() => wasReleasedDuringSave = isReleasing);

        await editor.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.True(wasReleasedDuringSave);
    }
}
