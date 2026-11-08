using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Library;
using Core.Playback;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class TrackEditorVm : ViewModelBase
{
    [ObservableProperty]
    public partial IAudioTrack? Track { get; private set; }

    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    [ObservableProperty]
    public partial string EditedTitle { get; set; } = "";

    [ObservableProperty]
    public partial string ArtistQuery { get; set; } = "";

    [ObservableProperty]
    public partial int SelectedSuggestionIndex { get; set; } = -1;

    [ObservableProperty]
    public partial bool HasSuggestions { get; private set; }

    [ObservableProperty]
    public partial bool IsTitleChanged { get; private set; }

    [ObservableProperty]
    public partial bool AreArtistsChanged { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsEditable { get; private set; } = true;

    public ObservableCollection<string> EditedArtists { get; } = [];
    public ObservableCollection<ArtistSuggestion> Suggestions { get; } = [];
    public LargeCoverVm LargeCover { get; }
    private const int MaxSuggestions = 8;
    private readonly MusicLibrary musicLibrary;
    private readonly TrackTagWriter tagWriter;
    private readonly IAudioPlayer audioPlayer;

    public TrackEditorVm(
        MusicLibrary musicLibrary,
        TrackTagWriter tagWriter,
        IAudioPlayer audioPlayer,
        ICoverCache coverCache,
        IUiDispatcher uiDispatcher
    )
    {
        this.musicLibrary = musicLibrary;
        this.tagWriter = tagWriter;
        this.audioPlayer = audioPlayer;
        LargeCover = new LargeCoverVm(coverCache.LoadLargeAsync, uiDispatcher);

        EditedArtists.CollectionChanged += (_, _) => UpdateChanges();
        audioPlayer.StateChanged += _ => uiDispatcher.Post(UpdateIsEditable);
    }

    public void Open(IAudioTrack track, Bitmap? preview)
    {
        EditedTitle = track.Metadata.Title;
        EditedArtists.Clear();

        foreach(string artist in track.Metadata.Artists)
        {
            EditedArtists.Add(artist);
        }

        ArtistQuery = "";
        Track = track;
        IsOpen = true;
        LargeCover.Load(track.AudioPath, preview);
    }

    [RelayCommand(CanExecute = nameof(IsEditable))]
    public async Task Save()
    {
        var track = Track;

        if(track == null)
        {
            return;
        }

        bool isSaved = await tagWriter.EditAsync(track, EditedTitle, EditedArtists.Append(ArtistQuery).ToList());

        if(isSaved && IsOpen && Track == track)
        {
            Close();
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        Close();
    }

    public void RemoveArtist(string artist)
    {
        EditedArtists.Remove(artist);
        UpdateSuggestions();
    }

    public void MoveArtist(int oldIndex, int newIndex)
    {
        EditedArtists.Move(oldIndex, newIndex);
    }

    public void RemoveLastArtist()
    {
        if(ArtistQuery == "" && EditedArtists.Count > 0)
        {
            EditedArtists.RemoveAt(EditedArtists.Count - 1);
        }
    }

    public void MoveSuggestionSelection(int offset)
    {
        if(Suggestions.Count > 0)
        {
            SelectedSuggestionIndex = Math.Clamp(SelectedSuggestionIndex + offset, 0, Suggestions.Count - 1);
        }
    }

    public void AddSelectedSuggestion()
    {
        if(SelectedSuggestionIndex < 0 || SelectedSuggestionIndex >= Suggestions.Count)
        {
            return;
        }

        EditedArtists.Add(Suggestions[SelectedSuggestionIndex].Name);
        ArtistQuery = "";
    }

    partial void OnEditedTitleChanged(string value)
    {
        UpdateChanges();
    }

    partial void OnArtistQueryChanged(string value)
    {
        UpdateSuggestions();
        UpdateChanges();
    }

    partial void OnTrackChanged(IAudioTrack? value)
    {
        UpdateIsEditable();
        UpdateChanges();
    }

    private void UpdateChanges()
    {
        IsTitleChanged = Track != null && EditedTitle != Track.Metadata.Title;
        AreArtistsChanged = Track != null
            && (ArtistQuery.Trim() != "" || !EditedArtists.SequenceEqual(Track.Metadata.Artists));
    }

    private void UpdateIsEditable()
    {
        var state = audioPlayer.State;
        IsEditable = Track == null
            || state.Status != PlaybackStatus.Playing
            || state.Track?.AudioTrack.AudioPath != Track.AudioPath;
    }

    private void Close()
    {
        ArtistQuery = "";
        IsOpen = false;
    }

    private void UpdateSuggestions()
    {
        Suggestions.Clear();
        string query = ArtistQuery.Trim();

        if(query != "")
        {
            var artists = musicLibrary.FindArtistsByPrefix(query);

            foreach(var artist in artists.Where(it => !IsAdded(it.Name)).Take(MaxSuggestions))
            {
                Suggestions.Add(CreateSuggestion(artist, query.Length));
            }

            bool isKnown = IsAdded(query)
                || artists.Any(it => string.Equals(it.Name, query, StringComparison.OrdinalIgnoreCase));

            if(!isKnown)
            {
                Suggestions.Add(new ArtistSuggestion()
                {
                    Name = query,
                    Rest = $"Add “{query}” as new artist",
                    IsNew = true
                });
            }
        }

        SelectedSuggestionIndex = Suggestions.Count > 0 ? 0 : -1;
        HasSuggestions = Suggestions.Count > 0;
    }

    private bool IsAdded(string artist)
    {
        return EditedArtists.Contains(artist, StringComparer.OrdinalIgnoreCase);
    }

    private static ArtistSuggestion CreateSuggestion(Artist artist, int matchLength)
    {
        int length = Math.Min(matchLength, artist.Name.Length);
        int trackCount = artist.Tracks.Count;

        return new ArtistSuggestion()
        {
            Name = artist.Name,
            Match = artist.Name.Substring(0, length),
            Rest = artist.Name.Substring(length),
            Details = trackCount == 1 ? "1 track" : $"{trackCount} tracks"
        };
    }
}
