using Core.Logging;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging;

namespace Core.Library;

public class MusicLibrary
{
    public Playlist Playlist { get; } = new Playlist();
    private readonly ILogger<MusicLibrary> logger;
    private readonly ITrackLoader trackLoader;
    private readonly LoudnessScanner loudnessScanner;
    private readonly HashSet<IAudioTrack> tracks = [];
    private readonly Lock artistsLock = new Lock();

    private readonly Dictionary<string, Artist> artists =
        new Dictionary<string, Artist>(StringComparer.OrdinalIgnoreCase);

    public MusicLibrary(
        ILogger<MusicLibrary> logger,
        ITrackLoader trackLoader,
        LoudnessScanner loudnessScanner,
        TrackTagWriter tagWriter)
    {
        this.logger = logger;
        this.trackLoader = trackLoader;
        this.loudnessScanner = loudnessScanner;

        tagWriter.TrackChanged += OnTrackChanged;
    }

    public Task LoadAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                var audioTracks = trackLoader.Load();
                AddToArtistIndex(audioTracks);
                Playlist.AddTracks(audioTracks);
                await loudnessScanner.ScanAsync(audioTracks).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                logger.Error(ex, $"Failed to load tracks");
            }
        });
    }

    public List<Artist> FindArtistsByPrefix(string prefix)
    {
        lock(artistsLock)
        {
            return artists.Values
                .Where(it => it.Name.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                .OrderByDescending(it => it.Tracks.Count)
                .ThenBy(it => it.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }

    private void AddToArtistIndex(IEnumerable<IAudioTrack> audioTracks)
    {
        lock(artistsLock)
        {
            foreach(var audioTrack in audioTracks)
            {
                tracks.Add(audioTrack);
                AddToArtists(audioTrack);
            }
        }
    }

    private void OnTrackChanged(IAudioTrack audioTrack)
    {
        lock(artistsLock)
        {
            if(!tracks.Contains(audioTrack))
            {
                return;
            }

            foreach(var artist in artists.Values.Where(it => it.Tracks.Contains(audioTrack)).ToList())
            {
                var remainingTracks = artist.Tracks.Where(it => it != audioTrack).ToList();

                if(remainingTracks.Count == 0)
                {
                    artists.Remove(artist.Name);
                }
                else
                {
                    artists[artist.Name] = new Artist(artist.Name, remainingTracks);
                }
            }

            AddToArtists(audioTrack);
        }
    }

    private void AddToArtists(IAudioTrack audioTrack)
    {
        foreach(string name in audioTrack.Metadata.Artists.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            artists[name] = artists.TryGetValue(name, out var artist)
                ? new Artist(artist.Name, artist.Tracks.Append(audioTrack).ToList())
                : new Artist(name, [audioTrack]);
        }
    }
}
