using Core.Playback;
using Core.Storage;

namespace Core.Tests.Playback;

public class AudioTrackTests
{
    [Fact]
    public void Constructor_AssignsProperties()
    {
        var metadata = new Metadata();
        var properties = new TrackProperties(TimeSpan.Zero);
        var track = new AudioTrack("path/to/track.mp3", metadata, properties);

        Assert.Equal("path/to/track.mp3", track.AudioPath);
        Assert.Equal(metadata, track.Metadata);
        Assert.Equal(properties, track.Properties);
    }

    [Fact]
    public void ToString_TitleAndArtistsSpecified_ReturnsArtistsAndTitle()
    {
        var metadata = new Metadata { Title = "Weightless", Artists = "Ari Xorka" };
        var properties = new TrackProperties(TimeSpan.Zero);
        var track = new AudioTrack("track.mp3", metadata, properties);

        Assert.Equal("Ari Xorka - Weightless", track.ToString());
    }

    [Fact]
    public void ToString_NoTitle_ReturnsFileNameWithoutExtension()
    {
        var metadata = new Metadata { Title = "", Artists = "Voyage" };
        var properties = new TrackProperties(TimeSpan.Zero);
        var track = new AudioTrack("path/to/Voyage_Paradise.flac", metadata, properties);

        Assert.Equal("Voyage_Paradise", track.ToString());
    }

    [Fact]
    public void ToString_NoArtists_ReturnsFileNameWithoutExtension()
    {
        var metadata = new Metadata { Title = "Enter Sandman", Artists = "" };
        var properties = new TrackProperties(TimeSpan.Zero);
        var track = new AudioTrack("path/to/Enter Sandman (Metallica).wav", metadata, properties);

        Assert.Equal("Enter Sandman (Metallica)", track.ToString());
    }
}
