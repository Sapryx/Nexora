using Core.Playback;

namespace Core.Tests.Playback;

public class AudioTrackTests
{
    [Fact]
    public void Constructor_AssignsProperties()
    {
        var metadata = new Metadata();
        var track = new AudioTrack("path/to/track.mp3", metadata);

        Assert.Equal("path/to/track.mp3", track.AudioPath);
        Assert.Equal(metadata, track.Metadata);
    }

    [Fact]
    public void ToString_TitleAndArtistsSpecified_ReturnsArtistsAndTitle()
    {
        var metadata = new Metadata { Title = "Weightless", Artists = "Ari Xorka" };
        var track = new AudioTrack("track.mp3", metadata);

        Assert.Equal("Ari Xorka - Weightless", track.ToString());
    }

    [Fact]
    public void ToString_NoTitle_ReturnsFileNameWithoutExtension()
    {
        var metadata = new Metadata { Title = "", Artists = "Voyage" };
        var track = new AudioTrack("path/to/Voyage_Paradise.flac", metadata);

        Assert.Equal("Voyage_Paradise", track.ToString());
    }

    [Fact]
    public void ToString_NoArtists_ReturnsFileNameWithoutExtension()
    {
        var metadata = new Metadata { Title = "Enter Sandman", Artists = "" };
        var track = new AudioTrack("path/to/Enter Sandman (Metallica).wav", metadata);

        Assert.Equal("Enter Sandman (Metallica)", track.ToString());
    }
}
