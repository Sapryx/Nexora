using Core.Library;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class TrackEditorVmTests
{
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly TrackMetadataEditor metadataEditor = new TrackMetadataEditor(
        NullLogger<TrackMetadataEditor>.Instance,
        Mock.Of<IMetadataSaver>(),
        Mock.Of<IPendingMetadataStorage>(),
        Mock.Of<IAudioPlayer>()
    );
    private readonly ArtistRegistry artistRegistry;
    private readonly TrackEditorVm vm;

    public TrackEditorVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        artistRegistry = new ArtistRegistry(metadataEditor);
        vm = new TrackEditorVm(artistRegistry);
    }

    [Fact]
    public void EditCommand_TrackExecutes_OpensTrackWithFieldsCopied()
    {
        var trackVm = CreateTrackVm("Under Pressure", "Queen", "David Bowie");

        trackVm.EditCommand.Execute(null);

        Assert.Same(trackVm, vm.TrackVm);
        Assert.Equal("Under Pressure", vm.EditedTitle);
        Assert.Equal(["Queen", "David Bowie"], vm.EditedArtists);
    }

    [Fact]
    public async Task Save_FieldsEdited_AppliesEditAndCloses()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        vm.EditedTitle = "Uprising";
        vm.EditedArtists.Add("Queen");

        await vm.Save();

        Assert.Null(vm.TrackVm);
        Assert.Equal("Uprising", trackVm.Title);
        Assert.Equal("Muse, Queen", trackVm.Artists);
    }

    [Fact]
    public async Task Save_ArtistQueryTyped_AddsItAsArtist()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        vm.ArtistQuery = "Queen";

        await vm.Save();

        Assert.Equal("Muse, Queen", trackVm.Artists);
        Assert.False(vm.HasSuggestions);
    }

    [Fact]
    public void Cancel_FieldsEdited_DiscardsEditAndCloses()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        vm.EditedTitle = "Uprising";
        vm.EditedArtists.Clear();

        vm.CancelCommand.Execute(null);

        Assert.Null(vm.TrackVm);
        Assert.Equal("Psycho", trackVm.Title);
        Assert.Equal("Muse", trackVm.Artists);
    }

    [Fact]
    public void ArtistQuery_PrefixOfKnownArtist_SuggestsArtistThenNewArtist()
    {
        AddLibraryTrack("Bohemian Rhapsody", "Queen");
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.ArtistQuery = "qu";

        Assert.Equal(["Queen", "qu"], vm.Suggestions.Select(it => it.Name));
        Assert.Equal("Qu", vm.Suggestions[0].Match);
        Assert.Equal("een", vm.Suggestions[0].Rest);
        Assert.Equal("1 track", vm.Suggestions[0].Details);
        Assert.True(vm.Suggestions[1].IsNew);
        Assert.Equal(0, vm.SelectedSuggestionIndex);
        Assert.True(vm.HasSuggestions);
    }

    [Fact]
    public void ArtistQuery_WholeNameOfKnownArtist_DoesNotSuggestNewArtist()
    {
        AddLibraryTrack("Bohemian Rhapsody", "Queen");
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.ArtistQuery = "queen";

        Assert.Equal(["Queen"], vm.Suggestions.Select(it => it.Name));
    }

    [Fact]
    public void ArtistQuery_ArtistAlreadyAdded_IsNotSuggested()
    {
        AddLibraryTrack("Hysteria", "Muse");
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.ArtistQuery = "Muse";

        Assert.Empty(vm.Suggestions);
        Assert.False(vm.HasSuggestions);
    }

    [Fact]
    public void AddSelectedSuggestion_SuggestionSelected_AddsArtistAndClearsQuery()
    {
        AddLibraryTrack("Bohemian Rhapsody", "Queen");
        vm.Open(CreateTrackVm("Psycho", "Muse"));
        vm.ArtistQuery = "Qu";

        vm.AddSelectedSuggestion();

        Assert.Equal(["Muse", "Queen"], vm.EditedArtists);
        Assert.Equal("", vm.ArtistQuery);
        Assert.Empty(vm.Suggestions);
    }

    [Fact]
    public void MoveSuggestionSelection_PastLastSuggestion_StaysOnLast()
    {
        AddLibraryTrack("Bohemian Rhapsody", "Queen");
        vm.Open(CreateTrackVm("Psycho", "Muse"));
        vm.ArtistQuery = "Qu";

        vm.MoveSuggestionSelection(1);
        vm.MoveSuggestionSelection(1);

        Assert.Equal(1, vm.SelectedSuggestionIndex);
    }

    [Fact]
    public void RemoveLastArtist_QueryEmpty_RemovesLastArtist()
    {
        vm.Open(CreateTrackVm("Under Pressure", "Queen", "David Bowie"));

        vm.RemoveLastArtist();

        Assert.Equal(["Queen"], vm.EditedArtists);
    }

    [Fact]
    public void RemoveLastArtist_QueryTyped_KeepsArtists()
    {
        vm.Open(CreateTrackVm("Under Pressure", "Queen", "David Bowie"));
        vm.ArtistQuery = "F";

        vm.RemoveLastArtist();

        Assert.Equal(["Queen", "David Bowie"], vm.EditedArtists);
    }

    private void AddLibraryTrack(string title, string artist)
    {
        var metadata = new Metadata() { Title = title, Artists = [artist] };
        artistRegistry.AddTracks([new AudioTrack($"{title}.mp3", metadata)]);
    }

    private TrackControlVm CreateTrackVm(string title, params string[] artists)
    {
        var metadata = new Metadata() { Title = title, Artists = artists };
        var playlistItem = Mock.Of<IPlaylistItem>(
            it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == $"{title}.mp3"
        );

        return new TrackControlVm(
            playlistItem,
            new PlaybackQueue(Mock.Of<IAudioPlayer>()),
            Mock.Of<IAudioPlayer>(),
            metadataEditor,
            vm,
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);
    }
}
