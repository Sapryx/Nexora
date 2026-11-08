using Core.Input;
using Core.Library;
using Core.Search;
using Core.Tests.Input;
using Moq;

namespace Core.Tests.Search;

public class TrackSearchQueryTests
{
    private readonly IAudioTrack enterSandman = CreateTrack("Enter Sandman", "Metallica");
    private readonly IAudioTrack complicated = CreateTrack("Complicated", "Avril Lavigne");
    private readonly IAudioTrack kukushka = CreateTrack("Кукушка", "Кино");
    private readonly KeyboardLayoutTranslator noLayoutsTranslator = TestKeyboardLayouts.CreateTranslator();
    private readonly KeyboardLayoutTranslator englishRussianTranslator = TestKeyboardLayouts.CreateTranslator(
        TestKeyboardLayouts.English,
        TestKeyboardLayouts.Russian
    );

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Match_EmptyQuery_ReturnsMatch(string rawQuery)
    {
        Assert.NotNull(Query(rawQuery).Match(enterSandman));
    }

    [Fact]
    public void Match_TitleContainsQuery_ReturnsMatch()
    {
        Assert.NotNull(Query("sand").Match(enterSandman));
    }

    [Fact]
    public void Match_ArtistsContainQuery_ReturnsMatch()
    {
        Assert.NotNull(Query("tall").Match(enterSandman));
    }

    [Fact]
    public void Match_DifferentCaseAndSurroundingSpaces_ReturnsMatch()
    {
        Assert.NotNull(Query("  ENTER ").Match(enterSandman));
    }

    [Fact]
    public void Match_TypoInFourCharacterQuery_ReturnsNull()
    {
        Assert.Null(Query("tell").Match(enterSandman));
    }

    [Fact]
    public void Match_TypoInFiveCharacterQuery_ReturnsMatch()
    {
        Assert.NotNull(Query("avrel").Match(complicated));
    }

    [Fact]
    public void Match_OneTypoInNineCharacterQuery_ReturnsMatch()
    {
        Assert.NotNull(Query("metallika").Match(enterSandman));
    }

    [Fact]
    public void Match_TwoTyposInNineCharacterQuery_ReturnsNull()
    {
        Assert.Null(Query("mxtallika").Match(enterSandman));
    }

    [Fact]
    public void Match_TwoTyposInTenCharacterQuery_ReturnsMatch()
    {
        Assert.NotNull(Query("avrel lavu").Match(complicated));
    }

    [Fact]
    public void Match_MissingLetter_ReturnsMatch()
    {
        Assert.NotNull(Query("metalica").Match(enterSandman));
    }

    [Fact]
    public void Match_ExtraLetter_ReturnsMatch()
    {
        Assert.NotNull(Query("sandmman").Match(enterSandman));
    }

    [Fact]
    public void Match_TransposedAdjacentLetters_ReturnsMatch()
    {
        Assert.NotNull(Query("metlalica").Match(enterSandman));
    }

    [Fact]
    public void Match_MissingLetterInFourCharacterQuery_ReturnsNull()
    {
        Assert.Null(Query("sadm").Match(enterSandman));
    }

    [Fact]
    public void Match_MissingAndExtraLetterInNineCharacterQuery_ReturnsNull()
    {
        Assert.Null(Query("mettalica").Match(enterSandman));
    }

    [Fact]
    public void Match_WordsFromTitleAndArtists_ReturnsMatch()
    {
        Assert.NotNull(Query("metallica sandman").Match(enterSandman));
    }

    [Fact]
    public void Match_WordsInReversedOrder_ReturnsMatch()
    {
        Assert.NotNull(Query("sandman enter").Match(enterSandman));
    }

    [Fact]
    public void Match_TypoInOneOfWords_ReturnsMatch()
    {
        Assert.NotNull(Query("sandman metalica").Match(enterSandman));
    }

    [Fact]
    public void Match_TypoInShortWordOfPhrase_ReturnsMatch()
    {
        Assert.NotNull(Query("entr sandman").Match(enterSandman));
    }

    [Fact]
    public void Match_OneOfWordsNotFound_ReturnsNull()
    {
        Assert.Null(Query("metallica complicated").Match(enterSandman));
    }

    [Theory]
    [InlineData("beyonce", "Halo", "Beyoncé")]
    [InlineData("Beyoncé", "Halo", "Beyonce")]
    [InlineData("motorhead", "Ace of Spades", "Motörhead")]
    [InlineData("елка", "Ёлка", "Unknown")]
    public void Match_DiacriticsDiffer_ReturnsMatch(string rawQuery, string title, string artists)
    {
        Assert.NotNull(Query(rawQuery).Match(CreateTrack(title, artists)));
    }

    [Theory]
    [InlineData("im yours", "I'm Yours", "Jason Mraz")]
    [InlineData("acdc", "Thunderstruck", "AC/DC")]
    [InlineData("guns n roses", "Paradise City", "Guns N' Roses")]
    [InlineData("ACDC: thunderstruck!", "Thunderstruck", "AC/DC")]
    public void Match_PunctuationDiffers_ReturnsMatch(string rawQuery, string title, string artists)
    {
        Assert.NotNull(Query(rawQuery).Match(CreateTrack(title, artists)));
    }

    [Fact]
    public void Match_SpacesAroundRemovedPunctuation_ReturnsMatch()
    {
        Assert.NotNull(Query("rock and roll").Match(CreateTrack("Rock - and - Roll", "Unknown")));
    }

    [Fact]
    public void Match_QueryLongerThanFields_ReturnsNull()
    {
        Assert.Null(Query("enter sandman by metallica").Match(enterSandman));
    }

    [Fact]
    public void Match_NoFieldContainsQuery_ReturnsNull()
    {
        Assert.Null(Query("avril").Match(enterSandman));
    }

    [Theory]
    [InlineData("ьуефддшсф")]
    [InlineData("ЬУЕФДДШСФ")]
    [InlineData("утеук ыфтвьфт")]
    public void Match_LatinQueryTypedInCyrillicLayout_ReturnsMatch(string rawQuery)
    {
        Assert.NotNull(new TrackSearchQuery(rawQuery, englishRussianTranslator).Match(enterSandman));
    }

    [Theory]
    [InlineData("rbyj")]
    [InlineData("rereirf rbyj")]
    [InlineData("Rereirf")]
    public void Match_CyrillicQueryTypedInLatinLayout_ReturnsMatch(string rawQuery)
    {
        Assert.NotNull(new TrackSearchQuery(rawQuery, englishRussianTranslator).Match(kukushka));
    }

    [Fact]
    public void Match_TypoInQueryTypedInWrongLayout_ReturnsMatch()
    {
        Assert.NotNull(new TrackSearchQuery("ьуефдшсф", englishRussianTranslator).Match(enterSandman));
    }

    [Fact]
    public void Match_WrongLayoutWithoutLayoutTranslation_ReturnsNull()
    {
        Assert.Null(Query("ьуефддшсф").Match(enterSandman));
    }

    [Fact]
    public void Match_QueryInCorrectLayoutWithLayoutTranslation_ReturnsMatch()
    {
        Assert.NotNull(new TrackSearchQuery("metallica", englishRussianTranslator).Match(enterSandman));
    }

    [Theory]
    [InlineData("azali", "Night Drive", "AZALI", MatchKind.Exact)]
    [InlineData("azali", "Night Drive", "AZALI, Enna Alouette", MatchKind.WholeWords)]
    [InlineData("azali", "Night Drive", "Azalia", MatchKind.FieldPrefix)]
    [InlineData("azali", "Night Drive", "The Azalias", MatchKind.WordPrefix)]
    [InlineData("azali", "Night Drive", "Kazalis", MatchKind.Substring)]
    [InlineData("drive azali", "Night Drive", "AZALI", MatchKind.WholeWords)]
    [InlineData("geoxor d", "Dreams", "Geoxor", MatchKind.FieldPrefix)]
    [InlineData("geoxor d", "Bloodlust", "Geoxor", MatchKind.Substring)]
    public void Match_QueryPosition_ReturnsKind(string rawQuery, string title, string artists, MatchKind expectedKind)
    {
        Assert.Equal(expectedKind, Query(rawQuery).Match(CreateTrack(title, artists))?.Kind);
    }

    [Fact]
    public void Match_TypoInsideWordAndExactArtist_RanksExactArtistFirst()
    {
        var exactArtist = Query("azali").Match(CreateTrack("Night Drive", "AZALI"));
        var typoInsideWord = Query("azali").Match(
            CreateTrack("PROXIMA (feat. Kazari Tayu & Enna Alouette)", "Unknown")
        );

        Assert.NotNull(exactArtist);
        Assert.NotNull(typoInsideWord);
        Assert.Equal(1, typoInsideWord.Typos);
        Assert.True(exactArtist.CompareTo(typoInsideWord) < 0);
    }

    [Fact]
    public void Match_TypoInExactField_RanksBelowSubstringWithoutTypos()
    {
        var typoInExactField = Query("azali").Match(CreateTrack("Night Drive", "Azari"));
        var substring = Query("azali").Match(CreateTrack("Night Drive", "Kazalis"));

        Assert.NotNull(typoInExactField);
        Assert.NotNull(substring);
        Assert.True(substring.CompareTo(typoInExactField) < 0);
    }

    [Fact]
    public void Match_TypoFollowedByExactWordStart_ReturnsSubstringKind()
    {
        Assert.Equal(MatchKind.Substring, Query("azali").Match(CreateTrack("Kazari", "Unknown"))?.Kind);
    }

    [Fact]
    public void Match_QueryInTitleAndArtists_ReturnsTitleField()
    {
        Assert.Equal(MatchField.Title, Query("muse").Match(CreateTrack("Muse", "Muse"))?.Field);
    }

    [Fact]
    public void Match_SameKindInTitleAndArtists_RanksTitleFirst()
    {
        var inTitle = Query("muse").Match(CreateTrack("Muse", "Unknown"));
        var inArtists = Query("muse").Match(CreateTrack("Unknown", "Muse"));

        Assert.NotNull(inTitle);
        Assert.NotNull(inArtists);
        Assert.True(inTitle.CompareTo(inArtists) < 0);
    }

    [Fact]
    public void Match_SameKindInWholeTitleAndAcrossFields_RanksWholeTitleFirst()
    {
        var wholeTitle = Query("enter sandman").Match(CreateTrack("Enter Sandman", "Unknown"));
        var wordsAcrossFields = Query("enter sandman").Match(CreateTrack("Sandman", "Enter"));

        Assert.NotNull(wholeTitle);
        Assert.NotNull(wordsAcrossFields);
        Assert.Equal(MatchKind.Exact, wordsAcrossFields.Kind);
        Assert.True(wholeTitle.CompareTo(wordsAcrossFields) < 0);
    }

    [Fact]
    public void Match_ArtistAndTitleWordsAcrossFields_RanksAboveTitlePrefix()
    {
        var artistAndTitle = Query("geoxor dreams").Match(CreateTrack("Dreams", "Geoxor"));
        var titlePrefix = Query("geoxor dreams").Match(CreateTrack("Geoxor Dreams Remix", "Unknown"));

        Assert.NotNull(artistAndTitle);
        Assert.NotNull(titlePrefix);
        Assert.True(artistAndTitle.CompareTo(titlePrefix) < 0);
    }

    [Fact]
    public void Match_WordWithTyposAcrossFields_SumsTyposOfWords()
    {
        Assert.Equal(2, Query("metalica sandmen").Match(enterSandman)?.Typos);
    }

    [Fact]
    public void Match_QueryInTypedLayout_RanksAboveTranslatedLayout()
    {
        var query = new TrackSearchQuery("rbyj", englishRussianTranslator);
        var typedLayout = query.Match(CreateTrack("rbyj", "Unknown"));
        var translatedLayout = query.Match(CreateTrack("кино", "Unknown"));

        Assert.NotNull(typedLayout);
        Assert.NotNull(translatedLayout);
        Assert.Equal(0, typedLayout.LayoutIndex);
        Assert.True(typedLayout.CompareTo(translatedLayout) < 0);
    }

    [Fact]
    public void Match_EmptyQuery_ReturnsEqualMatches()
    {
        var first = Query("").Match(enterSandman);
        var second = Query("").Match(complicated);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(0, first.CompareTo(second));
    }

    private TrackSearchQuery Query(string rawQuery)
    {
        return new TrackSearchQuery(rawQuery, noLayoutsTranslator);
    }

    private static IAudioTrack CreateTrack(string title, string artists)
    {
        var metadata = new Metadata() { Title = title, Artists = [artists] };
        return Mock.Of<IAudioTrack>(it => it.Metadata == metadata);
    }
}
