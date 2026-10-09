using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Playlists;

public class PlaylistItemTests
{
    private readonly Playlist playlist;

    public PlaylistItemTests()
    {
        playlist = new Playlist();
        playlist.AddTracks([Mock.Of<IAudioTrack>(), Mock.Of<IAudioTrack>(), Mock.Of<IAudioTrack>()]);
    }

    [Fact]
    public void GetNext_FirstItem_ReturnsSecondItem()
    {
        var next = playlist.GetItem(0).GetNext();

        Assert.Same(playlist.GetItem(1), next);
    }

    [Fact]
    public void GetNext_LastItem_ReturnsNull()
    {
        var next = playlist.GetItem(2).GetNext();

        Assert.Null(next);
    }

    [Fact]
    public void GetNext_TrackAddedAfterLastItem_ReturnsNewItem()
    {
        var last = playlist.GetItem(2);

        playlist.AddTrack(Mock.Of<IAudioTrack>());

        Assert.Same(playlist.GetItem(3), last.GetNext());
    }

    [Fact]
    public void GetPrevious_LastItem_ReturnsPreviousItem()
    {
        var previous = playlist.GetItem(2).GetPrevious();

        Assert.Same(playlist.GetItem(1), previous);
    }

    [Fact]
    public void GetPrevious_FirstItem_ReturnsNull()
    {
        var previous = playlist.GetItem(0).GetPrevious();

        Assert.Null(previous);
    }

    [Fact]
    public void GetNextAndGetPrevious_SingleItem_ReturnNull()
    {
        var singlePlaylist = new Playlist();
        singlePlaylist.AddTrack(Mock.Of<IAudioTrack>());
        var item = singlePlaylist.GetItem(0);

        Assert.Null(item.GetNext());
        Assert.Null(item.GetPrevious());
    }
}
