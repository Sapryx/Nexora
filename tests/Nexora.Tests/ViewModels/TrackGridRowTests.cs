using Core.Library;
using Core.Playlists;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;
using Tests.Shared.Playback;

namespace Nexora.Tests.ViewModels;

public class TrackGridRowTests
{
    private readonly List<TrackGridRow> rows = [];

    [Fact]
    public void UpdateRows_NoRows_SplitsTracksLeftToRightTopToBottom()
    {
        var trackVms = CreateTrackVms("A", "B", "C", "D", "E");

        TrackGridRow.UpdateRows(rows, trackVms, 2);

        Assert.Equal([["A", "B"], ["C", "D"], ["E"]], RowTitles());
    }

    [Fact]
    public void UpdateRows_TrackAppended_ReplacesOnlyLastRow()
    {
        var trackVms = CreateTrackVms("A", "B", "C");
        TrackGridRow.UpdateRows(rows, trackVms, 2);
        var firstRow = rows[0];
        var lastRow = rows[1];

        TrackGridRow.UpdateRows(rows, trackVms.Concat(CreateTrackVms("D")).ToList(), 2);

        Assert.Same(firstRow, rows[0]);
        Assert.NotSame(lastRow, rows[1]);
        Assert.Equal([["A", "B"], ["C", "D"]], RowTitles());
    }

    [Fact]
    public void UpdateRows_FewerTracks_RemovesExtraRows()
    {
        var trackVms = CreateTrackVms("A", "B", "C", "D", "E");
        TrackGridRow.UpdateRows(rows, trackVms, 2);

        TrackGridRow.UpdateRows(rows, trackVms.Take(2).ToList(), 2);

        Assert.Equal([["A", "B"]], RowTitles());
    }

    [Fact]
    public void UpdateRows_ColumnCountChanged_RegroupsTracks()
    {
        var trackVms = CreateTrackVms("A", "B", "C", "D", "E");
        TrackGridRow.UpdateRows(rows, trackVms, 2);

        TrackGridRow.UpdateRows(rows, trackVms, 3);

        Assert.Equal([["A", "B", "C"], ["D", "E"]], RowTitles());
    }

    [Fact]
    public void UpdateRows_NoTracks_RemovesAllRows()
    {
        TrackGridRow.UpdateRows(rows, CreateTrackVms("A"), 2);

        TrackGridRow.UpdateRows(rows, [], 2);

        Assert.Empty(rows);
    }

    private List<List<string>> RowTitles()
    {
        return rows.Select(row => row.TrackVms.Select(it => it.Title).ToList()).ToList();
    }

    private static List<TrackControlVm> CreateTrackVms(params string[] titles)
    {
        return titles.Select(CreateTrackVm).ToList();
    }

    private static TrackControlVm CreateTrackVm(string title)
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
            tagWriter,
            new TrackEditorVm(
                new ArtistRegistry(tagWriter),
                AudioPlayerMock.Create().Object,
                Mock.Of<ICoverCache>(),
                Mock.Of<IUiDispatcher>()
            ),
            Mock.Of<ICoverCache>(),
            Mock.Of<IUiDispatcher>());
    }
}
