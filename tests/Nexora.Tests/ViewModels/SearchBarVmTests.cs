using Core.Playback;
using Core.Playlists;
using Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Nexora.ViewModels.Factories;

namespace Nexora.Tests.ViewModels;

public class SearchBarVmTests
{
    private readonly PlaylistRegistry playlistRegistry;
    private readonly Mock<ITrackControlVmFactory> trackControlVmFactoryMock;
    private readonly Mock<IUiDispatcher> uiDispatcherMock;
    private readonly SearchBarVm vm;

    public SearchBarVmTests()
    {
        playlistRegistry = new PlaylistRegistry();
        trackControlVmFactoryMock = new Mock<ITrackControlVmFactory>();
        uiDispatcherMock = new Mock<IUiDispatcher>();

        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        trackControlVmFactoryMock
            .Setup(it => it.Create(It.IsAny<IPlaylistItem>()))
            .Returns<IPlaylistItem>(it => new TrackControlVm(
                it,
                Mock.Of<IAudioPlayer>(),
                Mock.Of<ICoverCache>(),
                uiDispatcherMock.Object));

        var layoutTranslator = new KeyboardLayoutTranslator(
            Mock.Of<IKeyboardLayoutProvider>(it => it.GetLayouts() == Array.Empty<KeyboardLayout>()),
            NullLogger<KeyboardLayoutTranslator>.Instance);

        vm = new SearchBarVm(playlistRegistry, trackControlVmFactoryMock.Object, layoutTranslator, uiDispatcherMock.Object);
    }

    [Fact]
    public void ItemAdded_EmptyQuery_DisplaysTrack()
    {
        AddTrack("Psycho", "Muse");

        Assert.Equal(["Psycho"], DisplayedTitles());
    }

    [Fact]
    public void SearchQuery_TypoInArtist_DisplaysMatchingTracksOnly()
    {
        AddTrack("Enter Sandman", "Metallica");
        AddTrack("Complicated", "Avril Lavigne");

        vm.SearchQuery = "metallika";

        Assert.Equal(["Enter Sandman"], DisplayedTitles());
    }

    [Fact]
    public void SearchQuery_Cleared_DisplaysAllTracksInAddedOrder()
    {
        AddTrack("Enter Sandman", "Metallica");
        AddTrack("Complicated", "Avril Lavigne");
        AddTrack("Psycho", "Muse");
        vm.SearchQuery = "muse";

        vm.SearchQuery = "";

        Assert.Equal(["Enter Sandman", "Complicated", "Psycho"], DisplayedTitles());
    }

    [Fact]
    public void ClearSearchQuery_AfterSearch_ClearsQueryAndDisplaysAllTracks()
    {
        AddTrack("Enter Sandman", "Metallica");
        AddTrack("Psycho", "Muse");
        vm.SearchQuery = "muse";

        vm.ClearSearchQueryCommand.Execute(null);

        Assert.Equal("", vm.SearchQuery);
        Assert.Equal(["Enter Sandman", "Psycho"], DisplayedTitles());
    }

    [Fact]
    public void ItemAdded_QueryWithSurroundingSpaces_DisplaysMatchingTrack()
    {
        vm.SearchQuery = "  MUSE ";

        AddTrack("Psycho", "Muse");
        AddTrack("Complicated", "Avril Lavigne");

        Assert.Equal(["Psycho"], DisplayedTitles());
    }

    [Fact]
    public void SearchQuery_ExactArtistAndTypoInTitle_DisplaysExactArtistFirst()
    {
        AddTrack("PROXIMA (feat. Kazari Tayu & Enna Alouette)", "Unknown");
        AddTrack("Night Drive", "AZALI");
        AddTrack("Neon", "AZALI");

        vm.SearchQuery = "AZALI";

        Assert.Equal(["Night Drive", "Neon", "PROXIMA (feat. Kazari Tayu & Enna Alouette)"], DisplayedTitles());
    }

    [Fact]
    public void ItemAdded_BetterMatchAddedLater_InsertsItBeforeWorseMatches()
    {
        vm.SearchQuery = "AZALI";

        AddTrack("PROXIMA (feat. Kazari Tayu & Enna Alouette)", "Unknown");
        AddTrack("Night Drive", "AZALI");
        AddTrack("Kazalis", "Unknown");
        AddTrack("Neon", "AZALI");

        Assert.Equal(
            ["Night Drive", "Neon", "Kazalis", "PROXIMA (feat. Kazari Tayu & Enna Alouette)"],
            DisplayedTitles()
        );
    }

    [Fact]
    public void SearchQuery_LastWordIsTitleStart_DisplaysTitlesStartingWithItFirst()
    {
        AddTrack("Bloodlust", "Geoxor");
        AddTrack("DEAD", "Geoxor");
        AddTrack("Dreams", "Geoxor");

        vm.SearchQuery = "geoxor d";

        Assert.Equal(["DEAD", "Dreams", "Bloodlust"], DisplayedTitles());
    }

    private void AddTrack(string title, string artists)
    {
        var track = Mock.Of<IAudioTrack>(it =>
            it.Metadata == new Metadata() { Title = title, Artists = artists } &&
            it.AudioPath == $"{title}.mp3");

        playlistRegistry.GlobalPlaylist.AddTrack(track);
    }

    private List<string> DisplayedTitles()
    {
        return [.. vm.DisplayedAudioTrackVms.Select(it => it.Title)];
    }
}
