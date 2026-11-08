using Core.Library;
using Core.Playback.Loudness;
using Core.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Tests.Shared.Library;

public static class TestMusicLibrary
{
    public static MusicLibrary Create(TrackTagWriter tagWriter)
    {
        return Create(tagWriter, []);
    }

    public static MusicLibrary Create(TrackTagWriter tagWriter, IReadOnlyList<IAudioTrack> loadedTracks)
    {
        var trackLoaderMock = new Mock<ITrackLoader>();
        trackLoaderMock
            .Setup(it => it.Load())
            .Returns(() => loadedTracks.ToList());
        var loudnessScanner = new LoudnessScanner(
            NullLogger<LoudnessScanner>.Instance,
            Mock.Of<ILoudnessAnalyzer>(),
            tagWriter,
            Mock.Of<IDegreeOfParallelismProvider<LoudnessScanner>>(it => it.Value == 1)
        );

        return new MusicLibrary(
            NullLogger<MusicLibrary>.Instance,
            trackLoaderMock.Object,
            loudnessScanner,
            tagWriter
        );
    }
}
