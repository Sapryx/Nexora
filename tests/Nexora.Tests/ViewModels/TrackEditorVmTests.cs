using Core.Library;
using Core.Playback;
using Core.Playlists;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Tests.Shared.Library;
using Tests.Shared.Playback;

namespace Nexora.Tests.ViewModels;

public class TrackEditorVmTests
{
    private readonly Mock<IUiDispatcher> uiDispatcherMock = new Mock<IUiDispatcher>();
    private readonly Mock<IMetadataSaver> metadataSaverMock = new Mock<IMetadataSaver>();
    private readonly Mock<IAudioPlayer> audioPlayerMock = AudioPlayerMock.Create();
    private readonly Mock<ICoverCache> coverCacheMock = new Mock<ICoverCache>();
    private readonly TrackTagWriter tagWriter;
    private readonly List<IAudioTrack> libraryTracks = [];
    private readonly MusicLibrary musicLibrary;
    private readonly TrackEditorVm vm;

    public TrackEditorVmTests()
    {
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        tagWriter = new TrackTagWriter(
            NullLogger<TrackTagWriter>.Instance,
            metadataSaverMock.Object,
            audioPlayerMock.Object
        );
        musicLibrary = TestMusicLibrary.Create(tagWriter, libraryTracks);
        vm = new TrackEditorVm(
            musicLibrary,
            audioPlayerMock.Object,
            coverCacheMock.Object,
            uiDispatcherMock.Object
        );
    }

    [Fact]
    public void EditCommand_TrackExecutes_OpensTrackWithFieldsCopied()
    {
        var trackVm = CreateTrackVm("Under Pressure", "Queen", "David Bowie");

        trackVm.EditCommand.Execute(null);

        Assert.Same(trackVm, vm.TrackVm);
        Assert.True(vm.IsOpen);
        Assert.Equal("Under Pressure", vm.EditedTitle);
        Assert.Equal(["Queen", "David Bowie"], vm.EditedArtists);
    }

    [Fact]
    public void Open_Track_LoadsLargeCover()
    {
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        coverCacheMock.Verify(it => it.LoadLargeAsync("Psycho.mp3"), Times.Once);
    }

    [Fact]
    public async Task Save_FieldsEdited_AppliesEditAndCloses()
    {
        var trackVm = CreateTrackVm("Psycho", "Muse");
        vm.Open(trackVm);
        vm.EditedTitle = "Uprising";
        vm.EditedArtists.Add("Queen");

        await vm.Save();

        Assert.False(vm.IsOpen);
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

        Assert.True(vm.IsOpen);
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
    public void StateChanged_OpenTrackPaused_AllowsEditing()
    {
        var playlistItem = SetupPlaying("Psycho.mp3");
        vm.Open(CreateTrackVm("Psycho", "Muse"));
        var paused = PlaybackState.Paused(playlistItem);
        audioPlayerMock.SetupGet(it => it.State).Returns(paused);

        audioPlayerMock.Raise(it => it.StateChanged += null, paused);

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

        Assert.False(vm.IsOpen);
        Assert.Equal("Psycho", trackVm.Title);
        Assert.Equal("Muse", trackVm.Artists);
    }

    [Fact]
    public async Task ArtistQuery_PrefixOfKnownArtist_SuggestsArtistThenNewArtist()
    {
        await LoadLibraryTrackAsync("Bohemian Rhapsody", "Queen");
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
    public async Task ArtistQuery_WholeNameOfKnownArtist_DoesNotSuggestNewArtist()
    {
        await LoadLibraryTrackAsync("Bohemian Rhapsody", "Queen");
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.ArtistQuery = "queen";

        Assert.Equal(["Queen"], vm.Suggestions.Select(it => it.Name));
    }

    [Fact]
    public async Task ArtistQuery_ArtistAlreadyAdded_IsNotSuggested()
    {
        await LoadLibraryTrackAsync("Hysteria", "Muse");
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.ArtistQuery = "Muse";

        Assert.Empty(vm.Suggestions);
        Assert.False(vm.HasSuggestions);
    }

    [Fact]
    public async Task AddSelectedSuggestion_SuggestionSelected_AddsArtistAndClearsQuery()
    {
        await LoadLibraryTrackAsync("Bohemian Rhapsody", "Queen");
        vm.Open(CreateTrackVm("Psycho", "Muse"));
        vm.ArtistQuery = "Qu";

        vm.AddSelectedSuggestion();

        Assert.Equal(["Muse", "Queen"], vm.EditedArtists);
        Assert.Equal("", vm.ArtistQuery);
        Assert.Empty(vm.Suggestions);
    }

    [Fact]
    public async Task MoveSuggestionSelection_PastLastSuggestion_StaysOnLast()
    {
        await LoadLibraryTrackAsync("Bohemian Rhapsody", "Queen");
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

    [Fact]
    public void Open_Track_NoFieldsChanged()
    {
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        Assert.False(vm.IsTitleChanged);
        Assert.False(vm.AreArtistsChanged);
    }

    [Fact]
    public void EditedTitle_ChangedAndRestored_UpdatesIsTitleChanged()
    {
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.EditedTitle = "Uprising";
        Assert.True(vm.IsTitleChanged);

        vm.EditedTitle = "Psycho";
        Assert.False(vm.IsTitleChanged);
    }

    [Fact]
    public void EditedArtists_ArtistRemovedAndReadded_UpdatesAreArtistsChanged()
    {
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.RemoveArtist("Muse");
        Assert.True(vm.AreArtistsChanged);

        vm.EditedArtists.Add("Muse");
        Assert.False(vm.AreArtistsChanged);
    }

    [Fact]
    public void MoveArtist_ToStart_ReordersArtistsAndMarksChanged()
    {
        vm.Open(CreateTrackVm("Under Pressure", "Queen", "David Bowie"));

        vm.MoveArtist(1, 0);

        Assert.Equal(["David Bowie", "Queen"], vm.EditedArtists);
        Assert.True(vm.AreArtistsChanged);
    }

    [Fact]
    public void ArtistQuery_Typed_ArtistsChanged()
    {
        vm.Open(CreateTrackVm("Psycho", "Muse"));

        vm.ArtistQuery = "Queen";

        Assert.True(vm.AreArtistsChanged);
    }

    private IPlaylistItem SetupPlaying(string audioPath)
    {
        var playlistItem = Mock.Of<IPlaylistItem>(it => it.AudioTrack.AudioPath == audioPath);
        audioPlayerMock.SetupGet(it => it.State).Returns(PlaybackState.Playing(playlistItem));

        return playlistItem;
    }

    private Task LoadLibraryTrackAsync(string title, string artist)
    {
        var metadata = new Metadata() { Title = title, Artists = [artist] };
        libraryTracks.Add(new AudioTrack($"{title}.mp3", metadata));

        return musicLibrary.LoadAsync();
    }

    private TrackControlVm CreateTrackVm(string title, params string[] artists)
    {
        var metadata = new Metadata() { Title = title, Artists = artists };
        var playlistItem = Mock.Of<IPlaylistItem>(
            it => it.AudioTrack.Metadata == metadata && it.AudioTrack.AudioPath == $"{title}.mp3"
        );

        var registry = new TrackControlVmRegistry(
            new PlaybackQueue(AudioPlayerMock.Create().Object),
            AudioPlayerMock.Create().Object,
            tagWriter,
            vm,
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object);

        return registry.Create(playlistItem);
    }
}
