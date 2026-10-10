using Core.Playback;
using Core.Search;
using Moq;

namespace Core.Tests.Search;

public class TrackSearchQueryTests
{
    private readonly IAudioTrack enterSandman = CreateTrack("Enter Sandman", "Metallica");
    private readonly IAudioTrack complicated = CreateTrack("Complicated", "Avril Lavigne");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_EmptyQuery_ReturnsTrue(string rawQuery)
    {
        Assert.True(new TrackSearchQuery(rawQuery).Matches(enterSandman));
    }

    [Fact]
    public void Matches_TitleContainsQuery_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("sand").Matches(enterSandman));
    }

    [Fact]
    public void Matches_ArtistsContainQuery_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("tall").Matches(enterSandman));
    }

    [Fact]
    public void Matches_DifferentCaseAndSurroundingSpaces_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("  ENTER ").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInFourCharacterQuery_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("tell").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInFiveCharacterQuery_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("avrel").Matches(complicated));
    }

    [Fact]
    public void Matches_OneTypoInNineCharacterQuery_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("metallika").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TwoTyposInNineCharacterQuery_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("mxtallika").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TwoTyposInTenCharacterQuery_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("avrel lavu").Matches(complicated));
    }

    [Fact]
    public void Matches_MissingLetter_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("metalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_ExtraLetter_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("sandmman").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TransposedAdjacentLetters_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("metlalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_MissingLetterInFourCharacterQuery_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("sadm").Matches(enterSandman));
    }

    [Fact]
    public void Matches_MissingAndExtraLetterInNineCharacterQuery_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("mettalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_WordsFromTitleAndArtists_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("metallica sandman").Matches(enterSandman));
    }

    [Fact]
    public void Matches_WordsInReversedOrder_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("sandman enter").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInOneOfWords_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("sandman metalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInShortWordOfPhrase_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("entr sandman").Matches(enterSandman));
    }

    [Fact]
    public void Matches_OneOfWordsNotFound_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("metallica complicated").Matches(enterSandman));
    }

    [Theory]
    [InlineData("beyonce", "Halo", "Beyoncé")]
    [InlineData("Beyoncé", "Halo", "Beyonce")]
    [InlineData("motorhead", "Ace of Spades", "Motörhead")]
    [InlineData("елка", "Ёлка", "Unknown")]
    public void Matches_DiacriticsDiffer_ReturnsTrue(string rawQuery, string title, string artists)
    {
        Assert.True(new TrackSearchQuery(rawQuery).Matches(CreateTrack(title, artists)));
    }

    [Theory]
    [InlineData("im yours", "I'm Yours", "Jason Mraz")]
    [InlineData("acdc", "Thunderstruck", "AC/DC")]
    [InlineData("guns n roses", "Paradise City", "Guns N' Roses")]
    [InlineData("ACDC: thunderstruck!", "Thunderstruck", "AC/DC")]
    public void Matches_PunctuationDiffers_ReturnsTrue(string rawQuery, string title, string artists)
    {
        Assert.True(new TrackSearchQuery(rawQuery).Matches(CreateTrack(title, artists)));
    }

    [Fact]
    public void Matches_SpacesAroundRemovedPunctuation_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("rock and roll").Matches(CreateTrack("Rock - and - Roll", "Unknown")));
    }

    [Fact]
    public void Matches_QueryLongerThanFields_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("enter sandman by metallica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_NoFieldContainsQuery_ReturnsFalse()
    {
        Assert.False(new TrackSearchQuery("avril").Matches(enterSandman));
    }

    private static IAudioTrack CreateTrack(string title, string artists)
    {
        return Mock.Of<IAudioTrack>(it => it.Metadata == new Metadata() { Title = title, Artists = artists });
    }
}
