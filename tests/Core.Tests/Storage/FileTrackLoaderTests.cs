using System.Collections.Immutable;
using Core.Playback;
using Core.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

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
}
