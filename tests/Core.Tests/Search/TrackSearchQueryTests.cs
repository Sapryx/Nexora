using Core.Playback;
using Core.Search;
using Moq;

namespace Core.Tests.Search;

public class TrackSearchQueryTests
{
    private readonly IAudioTrack enterSandman = CreateTrack("Enter Sandman", "Metallica");
    private readonly IAudioTrack complicated = CreateTrack("Complicated", "Avril Lavigne");
    private readonly IAudioTrack kukushka = CreateTrack("Кукушка", "Кино");
    private readonly KeyboardLayoutTranslator noLayoutsTranslator = TestKeyboardLayouts.CreateTranslator();
    private readonly KeyboardLayoutTranslator englishRussianTranslator = TestKeyboardLayouts.CreateTranslator(TestKeyboardLayouts.English, TestKeyboardLayouts.Russian);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_EmptyQuery_ReturnsTrue(string rawQuery)
    {
        Assert.True(Query(rawQuery).Matches(enterSandman));
    }

    [Fact]
    public void Matches_TitleContainsQuery_ReturnsTrue()
    {
        Assert.True(Query("sand").Matches(enterSandman));
    }

    [Fact]
    public void Matches_ArtistsContainQuery_ReturnsTrue()
    {
        Assert.True(Query("tall").Matches(enterSandman));
    }

    [Fact]
    public void Matches_DifferentCaseAndSurroundingSpaces_ReturnsTrue()
    {
        Assert.True(Query("  ENTER ").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInFourCharacterQuery_ReturnsFalse()
    {
        Assert.False(Query("tell").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInFiveCharacterQuery_ReturnsTrue()
    {
        Assert.True(Query("avrel").Matches(complicated));
    }

    [Fact]
    public void Matches_OneTypoInNineCharacterQuery_ReturnsTrue()
    {
        Assert.True(Query("metallika").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TwoTyposInNineCharacterQuery_ReturnsFalse()
    {
        Assert.False(Query("mxtallika").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TwoTyposInTenCharacterQuery_ReturnsTrue()
    {
        Assert.True(Query("avrel lavu").Matches(complicated));
    }

    [Fact]
    public void Matches_MissingLetter_ReturnsTrue()
    {
        Assert.True(Query("metalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_ExtraLetter_ReturnsTrue()
    {
        Assert.True(Query("sandmman").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TransposedAdjacentLetters_ReturnsTrue()
    {
        Assert.True(Query("metlalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_MissingLetterInFourCharacterQuery_ReturnsFalse()
    {
        Assert.False(Query("sadm").Matches(enterSandman));
    }

    [Fact]
    public void Matches_MissingAndExtraLetterInNineCharacterQuery_ReturnsFalse()
    {
        Assert.False(Query("mettalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_WordsFromTitleAndArtists_ReturnsTrue()
    {
        Assert.True(Query("metallica sandman").Matches(enterSandman));
    }

    [Fact]
    public void Matches_WordsInReversedOrder_ReturnsTrue()
    {
        Assert.True(Query("sandman enter").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInOneOfWords_ReturnsTrue()
    {
        Assert.True(Query("sandman metalica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_TypoInShortWordOfPhrase_ReturnsTrue()
    {
        Assert.True(Query("entr sandman").Matches(enterSandman));
    }

    [Fact]
    public void Matches_OneOfWordsNotFound_ReturnsFalse()
    {
        Assert.False(Query("metallica complicated").Matches(enterSandman));
    }

    [Theory]
    [InlineData("beyonce", "Halo", "Beyoncé")]
    [InlineData("Beyoncé", "Halo", "Beyonce")]
    [InlineData("motorhead", "Ace of Spades", "Motörhead")]
    [InlineData("елка", "Ёлка", "Unknown")]
    public void Matches_DiacriticsDiffer_ReturnsTrue(string rawQuery, string title, string artists)
    {
        Assert.True(Query(rawQuery).Matches(CreateTrack(title, artists)));
    }

    [Theory]
    [InlineData("im yours", "I'm Yours", "Jason Mraz")]
    [InlineData("acdc", "Thunderstruck", "AC/DC")]
    [InlineData("guns n roses", "Paradise City", "Guns N' Roses")]
    [InlineData("ACDC: thunderstruck!", "Thunderstruck", "AC/DC")]
    public void Matches_PunctuationDiffers_ReturnsTrue(string rawQuery, string title, string artists)
    {
        Assert.True(Query(rawQuery).Matches(CreateTrack(title, artists)));
    }

    [Fact]
    public void Matches_SpacesAroundRemovedPunctuation_ReturnsTrue()
    {
        Assert.True(Query("rock and roll").Matches(CreateTrack("Rock - and - Roll", "Unknown")));
    }

    [Fact]
    public void Matches_QueryLongerThanFields_ReturnsFalse()
    {
        Assert.False(Query("enter sandman by metallica").Matches(enterSandman));
    }

    [Fact]
    public void Matches_NoFieldContainsQuery_ReturnsFalse()
    {
        Assert.False(Query("avril").Matches(enterSandman));
    }

    [Theory]
    [InlineData("ьуефддшсф")]
    [InlineData("ЬУЕФДДШСФ")]
    [InlineData("утеук ыфтвьфт")]
    public void Matches_LatinQueryTypedInCyrillicLayout_ReturnsTrue(string rawQuery)
    {
        Assert.True(new TrackSearchQuery(rawQuery, englishRussianTranslator).Matches(enterSandman));
    }

    [Theory]
    [InlineData("rbyj")]
    [InlineData("rereirf rbyj")]
    [InlineData("Rereirf")]
    public void Matches_CyrillicQueryTypedInLatinLayout_ReturnsTrue(string rawQuery)
    {
        Assert.True(new TrackSearchQuery(rawQuery, englishRussianTranslator).Matches(kukushka));
    }

    [Fact]
    public void Matches_TypoInQueryTypedInWrongLayout_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("ьуефдшсф", englishRussianTranslator).Matches(enterSandman));
    }

    [Fact]
    public void Matches_WrongLayoutWithoutLayoutTranslation_ReturnsFalse()
    {
        Assert.False(Query("ьуефддшсф").Matches(enterSandman));
    }

    [Fact]
    public void Matches_QueryInCorrectLayoutWithLayoutTranslation_ReturnsTrue()
    {
        Assert.True(new TrackSearchQuery("metallica", englishRussianTranslator).Matches(enterSandman));
    }

    private TrackSearchQuery Query(string rawQuery)
    {
        return new TrackSearchQuery(rawQuery, noLayoutsTranslator);
    }

    private static IAudioTrack CreateTrack(string title, string artists)
    {
        return Mock.Of<IAudioTrack>(it => it.Metadata == new Metadata() { Title = title, Artists = artists });
    }
}
