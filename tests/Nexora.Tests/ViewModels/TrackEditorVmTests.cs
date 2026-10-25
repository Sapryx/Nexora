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
    private readonly Mock<IMetadataSaver> metadataSaverMock = new Mock<IMetadataSaver>();
    private readonly Mock<IAudioPlayer> audioPlayerMock = new Mock<IAudioPlayer>();
    private readonly TrackMetadataEditor metadataEditor;
    private readonly ArtistRegistry artistRegistry;
    private readonly TrackEditorVm vm;

    public TrackEditorVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        metadataEditor = new TrackMetadataEditor(
            NullLogger<TrackMetadataEditor>.Instance,
            metadataSaverMock.Object,
            audioPlayerMock.Object
        );
        artistRegistry = new ArtistRegistry(metadataEditor);
        vm = new TrackEditorVm(artistRegistry, audioPlayerMock.Object, uiDispatcherMock.Object);
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
    public async Task Save_WriteFails_KeepsTrackOpenAndUnchanged()
    {
        metadataSaverMock
            .Setup(it => it.Save(It.IsAny<MetadataChange>()))
            .Throws(new IOException("File is locked"));
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        vm.EditedTitle = "Uprising";

        await vm.Save();

        Assert.Same(trackVm, vm.TrackVm);
        Assert.Equal("Psycho", trackVm.Title);
    }

    [Fact]
    public void Open_TrackPlaying_DisablesEditingAndSave()
    {
        SetupPlaying("Psycho.mp3");

        vm.Open(CreateTrackVm("Psycho", "Muse"));

        Assert.False(vm.IsEditable);
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Open_OtherTrackPlaying_AllowsEditing()
    {
        SetupPlaying("Madness.mp3");

        vm.Open(CreateTrackVm("Psycho", "Muse"));

        Assert.True(vm.IsEditable);
        Assert.True(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void PlaybackPaused_OpenTrackWasPlaying_AllowsEditing()
    {
        SetupPlaying("Psycho.mp3");
        vm.Open(CreateTrackVm("Psycho", "Muse"));
        audioPlayerMock.SetupGet(it => it.IsPlaying).Returns(false);

        audioPlayerMock.Raise(it => it.PlaybackPaused += null);

        Assert.True(vm.IsEditable);
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

    private void SetupPlaying(string audioPath)
    {
        var playlistItem = Mock.Of<IPlaylistItem>(it => it.AudioTrack.AudioPath == audioPath);
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(playlistItem);
        audioPlayerMock.SetupGet(it => it.IsPlaying).Returns(true);
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
