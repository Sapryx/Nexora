using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Playlists;

public class PlaylistTests
{
    [Fact]
    public void Constructor_NoTracks_IsEmpty()
    {
        var playlist = new Playlist();

        Assert.True(playlist.IsEmpty);
        Assert.Equal(0, playlist.TrackCount);
    }

    [Fact]
    public void AddTrack_SingleTrack_IncrementsTrackCount()
    {
        var playlist = new Playlist();

        playlist.AddTrack(Mock.Of<IAudioTrack>());

        Assert.False(playlist.IsEmpty);
        Assert.Equal(1, playlist.TrackCount);
    }

    [Fact]
    public void AddTrack_SeveralTracks_AssignsSequentialIndices()
    {
        var playlist = new Playlist();

        playlist.AddTrack(Mock.Of<IAudioTrack>());
        playlist.AddTrack(Mock.Of<IAudioTrack>());
        playlist.AddTrack(Mock.Of<IAudioTrack>());

        Assert.Equal([0, 1, 2], playlist.Select(it => it.Index));
    }

    [Fact]
    public void AddTrack_AnyTrack_RaisesItemAddedWithNewItem()
    {
        var playlist = new Playlist();
        var track = Mock.Of<IAudioTrack>();
        IPlaylistItem? addedItem = null;
        playlist.ItemAdded += it => addedItem = it;

        playlist.AddTrack(track);

        Assert.NotNull(addedItem);
        Assert.Same(track, addedItem.AudioTrack);
        Assert.Same(playlist, addedItem.Playlist);
        Assert.Same(playlist.GetItem(0), addedItem);
    }

    [Fact]
    public void AddTracks_SeveralTracks_KeepsOrder()
    {
        var playlist = new Playlist();
        var one = Mock.Of<IAudioTrack>();
        var two = Mock.Of<IAudioTrack>();
        var three = Mock.Of<IAudioTrack>();

        playlist.AddTracks([one, two, three]);

        Assert.Equal([one, two, three], playlist.Select(it => it.AudioTrack));
        Assert.Equal([one, two, three], playlist.GetAllItems().Select(it => it.AudioTrack));
    }

    [Fact]
    public void AddTracks_SeveralTracks_RaisesItemAddedForEachTrack()
    {
        var playlist = new Playlist();
        List<IPlaylistItem> addedItems = [];
        playlist.ItemAdded += it => addedItems.Add(it);

        playlist.AddTracks([Mock.Of<IAudioTrack>(), Mock.Of<IAudioTrack>()]);

        Assert.Equal(playlist.ToList(), addedItems);
    }

    [Fact]
    public void GetItem_ValidIndex_ReturnsItemAtThatIndex()
    {
        var playlist = new Playlist();
        var two = Mock.Of<IAudioTrack>();
        playlist.AddTracks([Mock.Of<IAudioTrack>(), two]);

        var item = playlist.GetItem(1);

        Assert.Same(two, item.AudioTrack);
        Assert.Equal(1, item.Index);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void GetItem_IndexOutOfRange_Throws(int index)
    {
        var playlist = new Playlist();
        playlist.AddTracks([Mock.Of<IAudioTrack>(), Mock.Of<IAudioTrack>()]);

        Assert.Throws<ArgumentOutOfRangeException>(() => playlist.GetItem(index));
    }

    [Fact]
    public void GetItem_EmptyPlaylist_Throws()
    {
        var playlist = new Playlist();

        Assert.Throws<ArgumentOutOfRangeException>(() => playlist.GetItem(0));
    }
}
