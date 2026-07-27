using System.Collections;
using Core.Playback;

namespace Core.Playlists;

public class Playlist : IEnumerable<PlaylistItem>
{
    public string Name = string.Empty;
    public bool IsEmpty => TrackCount == 0;
    public int TrackCount => items.Count;
    
    private readonly List<PlaylistItem> items = [];

    public event Action<PlaylistItem>? ItemAdded;

    public IEnumerable<PlaylistItem> GetAllItems()
    {
        return items;
    }

    public PlaylistItem GetItem(int index)
    {
        if(index < 0 || index > items.Count - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return items[index];
    }

    public void AddTrack(IAudioTrack audioTrack)
    {
        var item = new PlaylistItem(audioTrack, this, items.Count);
        items.Add(item);
        ItemAdded?.Invoke(item);
    }

    public void AddTracks(IEnumerable<IAudioTrack> audioTracks)
    {
        foreach(var audioTrack in audioTracks)
        {
            AddTrack(audioTrack);
        }
    }

    public void RemoveItem(int index)
    {
        if(IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        items.RemoveAt(index);
    }

    public IEnumerator<PlaylistItem> GetEnumerator()
    {
        foreach(var item in items)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
