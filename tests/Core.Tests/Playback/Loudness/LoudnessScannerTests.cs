using Core.Playback;
using Core.Playback.Loudness;
using Core.Storage;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Shared.Logging;

namespace Core.Tests.Playback.Loudness;

public class LoudnessScannerTests
{
    private readonly Mock<ILoudnessAnalyzer> loudnessAnalyzerMock = new Mock<ILoudnessAnalyzer>();
    private readonly Mock<IMetadataSaver> metadataSaverMock = new Mock<IMetadataSaver>();
    private readonly Mock<IDegreeOfParallelismProvider<LoudnessScanner>> degreeOfParallelismProviderMock =
        new Mock<IDegreeOfParallelismProvider<LoudnessScanner>>();
    private readonly LoudnessScanner scanner;

    public LoudnessScannerTests()
    {
        degreeOfParallelismProviderMock
            .SetupGet(it => it.Value)
            .Returns(1);

        scanner = CreateScanner(NullLogger<LoudnessScanner>.Instance);
    }

    [Fact]
    public void Scan_TrackWithLoudnessTags_DoesNotAnalyze()
    {
        var track = CreateTrack("/music/one.mp3", new TrackLoudness() { Lufs = -10 });

        scanner.Scan([track]);

        loudnessAnalyzerMock.Verify(it => it.Analyze(It.IsAny<string>()), Times.Never);
        metadataSaverMock.Verify(
            it => it.SaveLoudness(It.IsAny<string>(), It.IsAny<TrackLoudness>()),
            Times.Never
        );
    }

    [Fact]
    public void Scan_TrackWithoutLoudness_SetsAndSavesAnalyzedLoudness()
    {
        var loudness = new TrackLoudness() { Lufs = -10, PeakDb = -1 };
        var track = CreateTrack("/music/one.mp3", null);
        loudnessAnalyzerMock.Setup(it => it.Analyze("/music/one.mp3")).Returns(loudness);

        scanner.Scan([track]);

        Assert.Same(loudness, track.Metadata.Loudness);
        metadataSaverMock.Verify(it => it.SaveLoudness("/music/one.mp3", loudness), Times.Once);
    }

    [Fact]
    public void Scan_AnalyzerFindsNoAudio_DoesNotSaveLoudness()
    {
        var track = CreateTrack("/music/one.mp3", null);

        scanner.Scan([track]);

        Assert.Null(track.Metadata.Loudness);
        metadataSaverMock.Verify(
            it => it.SaveLoudness(It.IsAny<string>(), It.IsAny<TrackLoudness>()),
            Times.Never
        );
    }

    [Fact]
    public void Scan_AnalyzerThrowsForOneTrack_AnalyzesOthers()
    {
        var loudness = new TrackLoudness() { Lufs = -10 };
        var broken = CreateTrack("/music/broken.mp3", null);
        var track = CreateTrack("/music/one.mp3", null);
        loudnessAnalyzerMock.Setup(it => it.Analyze("/music/broken.mp3")).Throws(new InvalidOperationException());
        loudnessAnalyzerMock.Setup(it => it.Analyze("/music/one.mp3")).Returns(loudness);

        scanner.Scan([broken, track]);

        Assert.Null(broken.Metadata.Loudness);
        Assert.Same(loudness, track.Metadata.Loudness);
    }

    [Fact]
    public void Scan_AnalyzerThrows_LogsWarningWithFileAndException()
    {
        var exception = new InvalidOperationException();
        var logger = new TestLogger<LoudnessScanner>();
        loudnessAnalyzerMock.Setup(it => it.Analyze("/music/broken.mp3")).Throws(exception);

        CreateScanner(logger).Scan([CreateTrack("/music/broken.mp3", null)]);

        var warning = Assert.Single(logger.Entries, it => it.Level == LogLevel.Warning);
        Assert.Contains("/music/broken.mp3", warning.Message);
        Assert.Same(exception, warning.Exception);
    }

    [Fact]
    public void Scan_SavingLoudnessThrows_KeepsAnalyzedLoudness()
    {
        var loudness = new TrackLoudness() { Lufs = -10 };
        var track = CreateTrack("/music/one.mp3", null);
        loudnessAnalyzerMock.Setup(it => it.Analyze("/music/one.mp3")).Returns(loudness);
        metadataSaverMock
            .Setup(it => it.SaveLoudness("/music/one.mp3", loudness))
            .Throws(new IOException("Locked"));

        scanner.Scan([track]);

        Assert.Same(loudness, track.Metadata.Loudness);
    }

    [Fact]
    public void Scan_TracksWithoutLoudness_ReadsDegreeOfParallelismFromProvider()
    {
        scanner.Scan([CreateTrack("/music/one.mp3", null)]);

        degreeOfParallelismProviderMock.VerifyGet(it => it.Value, Times.AtLeastOnce);
    }

    private LoudnessScanner CreateScanner(ILogger<LoudnessScanner> logger)
    {
        return new LoudnessScanner(
            logger,
            loudnessAnalyzerMock.Object,
            metadataSaverMock.Object,
            degreeOfParallelismProviderMock.Object
        );
    }

    private static AudioTrack CreateTrack(string path, TrackLoudness? loudness)
    {
        return new AudioTrack(path, new Metadata() { Loudness = loudness });
    }
}
