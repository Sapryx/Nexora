using Core.Library;
using Core.Playback.Loudness;
using Core.Storage;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Shared.Logging;
using Tests.Shared.Playback;

namespace Core.Tests.Library;

public class MusicLibraryTests
{
    private readonly Mock<ITrackLoader> trackLoaderMock = new Mock<ITrackLoader>();
    private readonly Mock<ILoudnessAnalyzer> loudnessAnalyzerMock = new Mock<ILoudnessAnalyzer>();
    private readonly TestLogger<MusicLibrary> logger = new TestLogger<MusicLibrary>();
    private readonly TrackTagWriter tagWriter = new TrackTagWriter(
        NullLogger<TrackTagWriter>.Instance,
        Mock.Of<IMetadataSaver>(),
        AudioPlayerMock.Create().Object
    );
    private readonly MusicLibrary library;

    public MusicLibraryTests()
    {
        var loudnessScanner = new LoudnessScanner(
            NullLogger<LoudnessScanner>.Instance,
            loudnessAnalyzerMock.Object,
            tagWriter,
            Mock.Of<IDegreeOfParallelismProvider<LoudnessScanner>>(it => it.Value == 1)
        );
        library = new MusicLibrary(logger, trackLoaderMock.Object, loudnessScanner, tagWriter);
    }

    [Fact]
    public async Task LoadAsync_LoaderReturnsTracks_AddsThemToPlaylist()
    {
        var psycho = CreateTrack("Psycho", "Muse");
        var uprising = CreateTrack("Uprising", "Muse");
        SetupLoadedTracks(psycho, uprising);

        await library.LoadAsync();

        Assert.Equal([psycho, uprising], library.Playlist.Select(it => it.AudioTrack));
    }

    [Fact]
    public async Task LoadAsync_TrackWithoutLoudness_AnalyzesItAfterAddingToPlaylist()
    {
        var track = new AudioTrack("/music/Psycho.mp3", new Metadata() { Title = "Psycho" });
        var loudness = new TrackLoudness() { Lufs = -10 };
        int trackCountDuringAnalysis = 0;
        SetupLoadedTracks(track);
        loudnessAnalyzerMock
            .Setup(it => it.Analyze("/music/Psycho.mp3"))
            .Callback(() => trackCountDuringAnalysis = library.Playlist.TrackCount)
            .Returns(loudness);

        await library.LoadAsync();

        Assert.Equal(1, trackCountDuringAnalysis);
        Assert.Same(loudness, track.Metadata.Loudness);
    }

    [Fact]
    public async Task LoadAsync_LoaderThrows_LogsError()
    {
        var exception = new IOException("Music folder is unavailable");
        trackLoaderMock
            .Setup(it => it.Load())
            .Throws(exception);

        await library.LoadAsync();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public async Task FindArtistsByPrefix_TracksLoaded_ReturnsMatchingArtistsWithMoreTracksFirst()
    {
        SetupLoadedTracks(
            CreateTrack("Mr. Brightside", "The Killers"),
            CreateTrack("Take Me Out", "Franz Ferdinand"),
            CreateTrack("Bohemian Rhapsody", "Freddie Mercury"),
            CreateTrack("Mr. Bad Guy", "Freddie Mercury")
        );

        await library.LoadAsync();
        var artists = library.FindArtistsByPrefix("fr");

        Assert.Equal(["Freddie Mercury", "Franz Ferdinand"], artists.Select(it => it.Name));
        Assert.Equal(2, artists[0].Tracks.Count);
    }

    [Fact]
    public async Task FindArtistsByPrefix_SameArtistInDifferentCase_GroupsTracksUnderFirstName()
    {
        SetupLoadedTracks(CreateTrack("Psycho", "Muse"), CreateTrack("Hysteria", "MUSE"));

        await library.LoadAsync();
        var artist = Assert.Single(library.FindArtistsByPrefix("muse"));

        Assert.Equal("Muse", artist.Name);
        Assert.Equal(2, artist.Tracks.Count);
    }

    [Fact]
    public async Task TrackChanged_ArtistReplaced_MovesTrackAndRemovesEmptyArtist()
    {
        var track = CreateTrack("Under Pressure", "Queen");
        SetupLoadedTracks(track);
        await library.LoadAsync();

        await tagWriter.EditAsync(track, "Under Pressure", ["David Bowie"]);

        Assert.Empty(library.FindArtistsByPrefix("Queen"));
        Assert.Same(track, Assert.Single(Assert.Single(library.FindArtistsByPrefix("David")).Tracks));
    }

    [Fact]
    public async Task TrackChanged_TrackNotInLibrary_IsIgnored()
    {
        await tagWriter.EditAsync(CreateTrack("Psycho", "Muse"), "Psycho", ["Queen"]);

        Assert.Empty(library.FindArtistsByPrefix(""));
    }

    private void SetupLoadedTracks(params IAudioTrack[] audioTracks)
    {
        trackLoaderMock
            .Setup(it => it.Load())
            .Returns(audioTracks.ToList());
    }

    private static AudioTrack CreateTrack(string title, string artist)
    {
        return new AudioTrack(
            $"/music/{title}.mp3",
            new Metadata() { Title = title, Artists = [artist], Loudness = new TrackLoudness() { Lufs = -10 } }
        );
    }
}
