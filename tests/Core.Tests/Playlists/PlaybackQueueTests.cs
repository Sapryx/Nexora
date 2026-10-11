using Core.Playback;
using Core.Playlists;
using Moq;

namespace Core.Tests.Playlists;

public class PlaybackQueueTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock = new Mock<IAudioPlayer>();
    private readonly PlaybackQueue queue;

    public PlaybackQueueTests()
    {
        queue = new PlaybackQueue(audioPlayerMock.Object);
    }

    [Fact]
    public void RemoveItem_ItemNotPlaying_RemovesItKeepingPlayback()
    {
        queue.AddTrack(Mock.Of<IAudioTrack>());
        queue.AddTrack(Mock.Of<IAudioTrack>());
        var playing = queue.GetItem(0);
        var item = queue.GetItem(1);
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(playing);

        queue.RemoveItem(item);

        Assert.Equal([playing], queue);
        audioPlayerMock.Verify(it => it.PlayTrack(It.IsAny<IPlaylistItem>()), Times.Never);
        audioPlayerMock.Verify(it => it.Stop(), Times.Never);
    }

    [Fact]
    public void RemoveItem_PlayingItem_PlaysNextItemAndRemovesPlayingOne()
    {
        queue.AddTrack(Mock.Of<IAudioTrack>());
        queue.AddTrack(Mock.Of<IAudioTrack>());
        var item = queue.GetItem(0);
        var next = queue.GetItem(1);
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(item);

        queue.RemoveItem(item);

        Assert.Equal([next], queue);
        audioPlayerMock.Verify(it => it.PlayTrack(next));
    }

    [Fact]
    public void RemoveItem_PlayingItemThroughItsPlaylistReference_PlaysNextItem()
    {
        queue.AddTrack(Mock.Of<IAudioTrack>());
        queue.AddTrack(Mock.Of<IAudioTrack>());
        var item = queue.GetItem(0);
        var next = queue.GetItem(1);
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(item);

        item.Playlist.RemoveItem(item);

        audioPlayerMock.Verify(it => it.PlayTrack(next));
    }

    [Fact]
    public void RemoveItem_PlayingLastItem_StopsPlaybackAndRemovesIt()
    {
        queue.AddTrack(Mock.Of<IAudioTrack>());
        queue.AddTrack(Mock.Of<IAudioTrack>());
        var item = queue.GetItem(1);
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(item);

        queue.RemoveItem(item);

        Assert.Equal(1, queue.TrackCount);
        audioPlayerMock.Verify(it => it.Stop());
        audioPlayerMock.Verify(it => it.PlayTrack(It.IsAny<IPlaylistItem>()), Times.Never);
    }

    [Fact]
    public void RemoveItem_PlayingItemOfOtherPlaylist_KeepsPlaybackAndQueue()
    {
        queue.AddTrack(Mock.Of<IAudioTrack>());
        var otherPlaylist = new Playlist();
        otherPlaylist.AddTrack(Mock.Of<IAudioTrack>());
        var item = otherPlaylist.GetItem(0);
        audioPlayerMock.SetupGet(it => it.NowPlaying).Returns(item);

        queue.RemoveItem(item);

        Assert.Equal(1, queue.TrackCount);
        audioPlayerMock.Verify(it => it.Stop(), Times.Never);
    }
}
