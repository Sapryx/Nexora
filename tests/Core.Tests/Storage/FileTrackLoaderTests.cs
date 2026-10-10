using System.Collections.Immutable;
using Core.Playback;
using Core.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Shared.Logging;

namespace Core.Tests.Storage;

public class FileTrackLoaderTests
{
    private readonly Mock<IMetadataLoader> metadataLoaderMock;
    private readonly Mock<ISupportedAudioFormatsProvider> supportedAudioFormatsProviderMock;
    private readonly Mock<IDegreeOfParallelismProvider<FileTrackLoader>> degreeOfParallelismProviderMock;
    private readonly Mock<IMusicDirectoryProvider> musicDirectoryProviderMock;
    private readonly FileTrackLoader loader;

    public FileTrackLoaderTests()
    {
        metadataLoaderMock = new Mock<IMetadataLoader>();
        supportedAudioFormatsProviderMock = new Mock<ISupportedAudioFormatsProvider>();
        degreeOfParallelismProviderMock = new Mock<IDegreeOfParallelismProvider<FileTrackLoader>>();
        musicDirectoryProviderMock = new Mock<IMusicDirectoryProvider>();

        degreeOfParallelismProviderMock
            .SetupGet(it => it.Value)
            .Returns(1);

        metadataLoaderMock
            .Setup(it => it.Load(It.IsAny<string>()))
            .Returns(new Metadata());

        supportedAudioFormatsProviderMock
            .Setup(it => it.GetFormats())
            .Returns(ImmutableHashSet.Create(".mp3", ".flac"));

        loader = new FileTrackLoader(
            NullLogger<FileTrackLoader>.Instance,
            metadataLoaderMock.Object,
            supportedAudioFormatsProviderMock.Object,
            degreeOfParallelismProviderMock.Object,
            musicDirectoryProviderMock.Object);
    }

    [Fact]
    public void Load_NoFilesInMusicDirectory_ReturnsEmptyList()
    {
        musicDirectoryProviderMock.Setup(it => it.GetFiles()).Returns([]);

        var result = loader.Load();

        Assert.Empty(result);
    }

    [Fact]
    public void Load_OnlySupportedFormats_ReturnsTrackForEachFile()
    {
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.mp3", "/music/two.flac"]);

        var result = loader.Load();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, track => track.AudioPath == "/music/one.mp3");
        Assert.Contains(result, track => track.AudioPath == "/music/two.flac");
    }

    [Fact]
    public void Load_UnsupportedFormatPresent_FiltersItOut()
    {
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.mp3", "/music/notes.txt", "/music/cover.png"]);

        var result = loader.Load();

        Assert.Single(result);
        Assert.Equal("/music/one.mp3", result[0].AudioPath);
    }

    [Fact]
    public void Load_CaseDiffersFromRegisteredFormat_FiltersItOut()
    {
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.MP3"]);

        var result = loader.Load();

        Assert.Empty(result);
    }

    [Fact]
    public void Load_SupportedFile_LoadsMetadataForThatFile()
    {
        var metadata = new Metadata() { Title = "Title", Artists = "Artist" };
        metadataLoaderMock.Setup(it => it.Load("/music/one.mp3")).Returns(metadata);
        musicDirectoryProviderMock.Setup(it => it.GetFiles()).Returns(["/music/one.mp3"]);

        var result = loader.Load();

        metadataLoaderMock.Verify(it => it.Load("/music/one.mp3"), Times.Once);
        Assert.Same(metadata, result[0].Metadata);
    }

    [Fact]
    public void Load_UnsupportedFile_DoesNotLoadMetadata()
    {
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/notes.txt"]);

        loader.Load();

        metadataLoaderMock.Verify(it => it.Load(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Load_FilesFound_ReadsDegreeOfParallelismFromProvider()
    {
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.mp3"]);

        loader.Load();

        degreeOfParallelismProviderMock.VerifyGet(it => it.Value, Times.AtLeastOnce);
    }

    [Fact]
    public void Load_TracksWithDifferentTitles_ReturnsThemSortedByTitleIgnoringCase()
    {
        metadataLoaderMock.Setup(it => it.Load("/music/one.mp3")).Returns(new Metadata() { Title = "charlie" });
        metadataLoaderMock.Setup(it => it.Load("/music/two.mp3")).Returns(new Metadata() { Title = "Alpha" });
        metadataLoaderMock.Setup(it => it.Load("/music/three.mp3")).Returns(new Metadata() { Title = "bravo" });
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.mp3", "/music/two.mp3", "/music/three.mp3"]);

        var result = loader.Load();

        Assert.Equal(["Alpha", "bravo", "charlie"], result.Select(it => it.Metadata.Title));
    }

    [Fact]
    public void Load_TracksWithSameTitle_ReturnsThemSortedByArtists()
    {
        metadataLoaderMock.Setup(it => it.Load("/music/one.mp3")).Returns(new Metadata() { Title = "Song", Artists = "Zed" });
        metadataLoaderMock.Setup(it => it.Load("/music/two.mp3")).Returns(new Metadata() { Title = "Song", Artists = "Abba" });
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.mp3", "/music/two.mp3"]);

        var result = loader.Load();

        Assert.Equal(["Abba", "Zed"], result.Select(it => it.Metadata.Artists));
    }

    [Fact]
    public void Load_MetadataLoaderThrowsForOneFile_SkipsItAndReturnsOthers()
    {
        metadataLoaderMock.Setup(it => it.Load("/music/broken.mp3")).Throws(new IOException("Corrupted"));
        musicDirectoryProviderMock
            .Setup(it => it.GetFiles())
            .Returns(["/music/one.mp3", "/music/broken.mp3", "/music/two.mp3"]);

        var result = loader.Load();

        Assert.Equal(["/music/one.mp3", "/music/two.mp3"], result.Select(it => it.AudioPath).Order());
    }

    [Fact]
    public void Load_MetadataLoaderThrows_LogsWarningWithFileAndException()
    {
        var exception = new IOException("Corrupted");
        var logger = new TestLogger<FileTrackLoader>();
        var loaderWithLogger = new FileTrackLoader(
            logger,
            metadataLoaderMock.Object,
            supportedAudioFormatsProviderMock.Object,
            degreeOfParallelismProviderMock.Object,
            musicDirectoryProviderMock.Object);
        metadataLoaderMock.Setup(it => it.Load("/music/broken.mp3")).Throws(exception);
        musicDirectoryProviderMock.Setup(it => it.GetFiles()).Returns(["/music/broken.mp3"]);

        loaderWithLogger.Load();

        var warning = Assert.Single(logger.Entries, it => it.Level == LogLevel.Warning);
        Assert.Contains("/music/broken.mp3", warning.Message);
        Assert.Same(exception, warning.Exception);
    }
}
