using Core.Library;
using Core.Playback;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Core.Tests.Library;

public class ArtistRegistryTests
{
    private readonly TrackMetadataEditor metadataEditor = new TrackMetadataEditor(
        NullLogger<TrackMetadataEditor>.Instance,
        Mock.Of<IMetadataSaver>(),
        Mock.Of<IAudioPlayer>()
    );
    private readonly ArtistRegistry registry;

    public ArtistRegistryTests()
    {
        registry = new ArtistRegistry(metadataEditor);
    }

    [Fact]
    public void FindByPrefix_TracksAdded_ReturnsMatchingArtistsWithMoreTracksFirst()
    {
        registry.AddTracks([
            CreateTrack("Mr. Brightside", "The Killers"),
            CreateTrack("Take Me Out", "Franz Ferdinand"),
            CreateTrack("Bohemian Rhapsody", "Freddie Mercury"),
            CreateTrack("Mr. Bad Guy", "Freddie Mercury")
        ]);

        var artists = registry.FindByPrefix("fr");

        Assert.Equal(["Freddie Mercury", "Franz Ferdinand"], artists.Select(it => it.Name));
        Assert.Equal(2, artists[0].Tracks.Count);
    }

    [Fact]
    public void AddTracks_SameArtistInDifferentCase_GroupsTracksUnderFirstName()
    {
        registry.AddTracks([CreateTrack("Psycho", "Muse"), CreateTrack("Hysteria", "MUSE")]);

        var artist = Assert.Single(registry.FindByPrefix("muse"));

        Assert.Equal("Muse", artist.Name);
        Assert.Equal(2, artist.Tracks.Count);
    }

    [Fact]
    public async Task MetadataChanged_ArtistReplaced_MovesTrackAndRemovesEmptyArtist()
    {
        var track = CreateTrack("Under Pressure", "Queen");
        registry.AddTracks([track]);

        await metadataEditor.EditAsync(track, "Under Pressure", ["David Bowie"]);

        Assert.Empty(registry.FindByPrefix("Queen"));
        Assert.Same(track, Assert.Single(Assert.Single(registry.FindByPrefix("David")).Tracks));
    }

    [Fact]
    public async Task MetadataChanged_TrackNotAdded_IsIgnored()
    {
        await metadataEditor.EditAsync(CreateTrack("Psycho", "Muse"), "Psycho", ["Queen"]);

        Assert.Empty(registry.FindByPrefix(""));
    }

    private static AudioTrack CreateTrack(string title, string artist)
    {
        return new AudioTrack($"/music/{title}.mp3", new Metadata() { Title = title, Artists = [artist] });
    }
}
