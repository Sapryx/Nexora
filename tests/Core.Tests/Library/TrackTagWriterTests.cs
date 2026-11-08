using Core.Library;
using Core.Playback;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Shared.Logging;
using Tests.Shared.Playback;

namespace Core.Tests.Library;

public class TrackTagWriterTests
{
    private const string AudioPath = "/music/Psycho.mp3";
    private readonly Mock<IMetadataSaver> metadataSaverMock = new Mock<IMetadataSaver>();
    private readonly Mock<IAudioPlayer> audioPlayerMock = AudioPlayerMock.Create();
    private readonly TestLogger<TrackTagWriter> logger = new TestLogger<TrackTagWriter>();
    private readonly AudioTrack audioTrack = new AudioTrack(
        AudioPath,
        new Metadata() { Title = "Psycho", Artists = ["Muse"] }
    );
    private readonly List<IAudioTrack> changedTracks = [];
    private readonly TrackTagWriter writer;
    private bool isReleasing;

    public TrackTagWriterTests()
    {
        writer = new TrackTagWriter(logger, metadataSaverMock.Object, audioPlayerMock.Object);
        writer.TrackChanged += it => changedTracks.Add(it);
    }

    [Fact]
    public async Task EditAsync_NothingChanged_DoesNotSave()
    {
        bool isSaved = await writer.EditAsync(audioTrack, " Psycho ", ["Muse"]);

        Assert.True(isSaved);
        metadataSaverMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
        Assert.Empty(changedTracks);
    }

    [Fact]
    public async Task EditAsync_TitleChanged_SavesOnlyTitleAndShowsIt()
    {
        bool isSaved = await writer.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.True(isSaved);
        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change =>
            change.AudioPath == AudioPath && change.Title == "Uprising" && change.Artists == null)));
        Assert.Equal("Uprising", audioTrack.Metadata.Title);
        Assert.Equal([audioTrack], changedTracks);
    }

    [Fact]
    public async Task EditAsync_ArtistsChanged_SavesNormalizedArtistsOnly()
    {
        await writer.EditAsync(audioTrack, "Psycho", [" Muse", "Queen ", "", " ", "muse"]);

        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change =>
            change.Title == null && change.Artists != null)));
        Assert.Equal(["Muse", "Queen"], audioTrack.Metadata.Artists);
    }

    [Fact]
    public async Task EditAsync_TitleCleared_SavesEmptyTitleAndShowsFileName()
    {
        audioTrack.Metadata.Title = "Uprising";

        await writer.EditAsync(audioTrack, "  ", ["Muse"]);

        metadataSaverMock.Verify(it => it.Save(It.Is<MetadataChange>(change => change.Title == "")));
        Assert.Equal("Psycho", audioTrack.Metadata.Title);
    }

    [Fact]
    public async Task EditAsync_TitleClearedAndAlreadyFileName_DoesNotSave()
    {
        await writer.EditAsync(audioTrack, "", ["Muse"]);

        metadataSaverMock.Verify(it => it.Save(It.IsAny<MetadataChange>()), Times.Never);
    }

    [Fact]
    public async Task EditAsync_SaveFails_LogsErrorAndKeepsMetadata()
    {
        var exception = new IOException("File is locked");
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Throws(exception);

        bool isSaved = await writer.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.False(isSaved);
        Assert.Equal("Psycho", audioTrack.Metadata.Title);
        Assert.Empty(changedTracks);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(AudioPath, entry.Message);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public async Task EditAsync_TitleChanged_SavesWhileTrackFileIsReleased()
    {
        bool wasReleasedDuringSave = false;
        SetupFileRelease();
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Callback(() => wasReleasedDuringSave = isReleasing);

        await writer.EditAsync(audioTrack, "Uprising", ["Muse"]);

        Assert.True(wasReleasedDuringSave);
    }

    [Fact]
    public async Task SaveLoudnessAsync_Loudness_ShowsItAndSavesWhileTrackFileIsReleased()
    {
        var loudness = new TrackLoudness() { Lufs = -10, PeakDb = -1 };
        bool wasReleasedDuringSave = false;
        SetupFileRelease();
        metadataSaverMock
            .Setup(it => it.SaveLoudness(AudioPath, loudness))
            .Callback(() => wasReleasedDuringSave = isReleasing);

        await writer.SaveLoudnessAsync(audioTrack, loudness);

        Assert.Same(loudness, audioTrack.Metadata.Loudness);
        Assert.True(wasReleasedDuringSave);
    }

    [Fact]
    public async Task SaveLoudnessAsync_Loudness_DoesNotRaiseTrackChanged()
    {
        await writer.SaveLoudnessAsync(audioTrack, new TrackLoudness() { Lufs = -10 });

        Assert.Empty(changedTracks);
    }

    [Fact]
    public async Task SaveLoudnessAsync_SaveFails_ThrowsAndKeepsLoudness()
    {
        var loudness = new TrackLoudness() { Lufs = -10 };
        var exception = new IOException("File is locked");
        metadataSaverMock
            .Setup(it => it.SaveLoudness(AudioPath, loudness))
            .Throws(exception);

        var thrown = await Assert.ThrowsAsync<IOException>(() => writer.SaveLoudnessAsync(audioTrack, loudness));

        Assert.Same(exception, thrown);
        Assert.Same(loudness, audioTrack.Metadata.Loudness);
    }

    private void SetupFileRelease()
    {
        audioPlayerMock
            .Setup(it => it.ReleaseFileAsync(AudioPath, It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>(async (audioPath, action) =>
            {
                isReleasing = true;
                await action();
                isReleasing = false;
            });
    }
}
