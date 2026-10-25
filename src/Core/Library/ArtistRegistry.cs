using Core.Playback;
using Core.Storage;

namespace Core.Library;

public class ArtistRegistry
{
    private readonly Dictionary<string, Artist> artists =
        new Dictionary<string, Artist>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IAudioTrack> tracks = [];
    private readonly Lock artistsLock = new Lock();

    public ArtistRegistry(TrackMetadataEditor metadataEditor)
    {
        metadataEditor.MetadataChanged += OnMetadataChanged;
    }

    public void AddTracks(IEnumerable<IAudioTrack> audioTracks)
    {
        lock(artistsLock)
        {
            foreach(var audioTrack in audioTracks)
            {
                tracks[audioTrack.AudioPath] = audioTrack;
                AddToArtists(audioTrack);
            }
        }
    }

    public List<Artist> FindByPrefix(string prefix)
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

    private void OnMetadataChanged(string audioPath)
    {
        lock(artistsLock)
        {
            if(!tracks.TryGetValue(audioPath, out var audioTrack))
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
