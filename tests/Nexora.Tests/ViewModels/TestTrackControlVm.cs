using Core.Library;
using Core.Playlists;
using Core.Storage.Tags;
using Core.Tracks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Tests.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Tests.Shared.Library;
using Tests.Shared.Playback;

namespace Nexora.Tests.ViewModels;

public static class TestTrackControlVm
{
    public static TrackControlVm Create(string title)
    {
        return Create(title, Mock.Of<ICoverCache>(), TestCoverLoadQueue.Create());
    }

    public static TrackControlVm Create(string title, ICoverCache coverCache, CoverLoadQueue coverLoadQueue)
    {
        var audioTrack = Mock.Of<IAudioTrack>(it =>
            it.Metadata == new Metadata() { Title = title } &&
            it.AudioPath == $"{title}.mp3");
        var tagWriter = new TrackTagWriter(
            NullLogger<TrackTagWriter>.Instance,
            Mock.Of<IMetadataSaver>(),
            AudioPlayerMock.Create().Object
        );

        return new TrackControlVm(
            Mock.Of<IPlaylistItem>(it => it.AudioTrack == audioTrack),
            new PlaybackQueue(AudioPlayerMock.Create().Object),
            AudioPlayerMock.Create().Object,
            new TrackEditorVm(
                TestMusicLibrary.Create(tagWriter),
                tagWriter,
                AudioPlayerMock.Create().Object,
                Mock.Of<ICoverCache>(),
                Mock.Of<IUiDispatcher>()
            ),
            coverCache,
            coverLoadQueue
        );
    }
}
