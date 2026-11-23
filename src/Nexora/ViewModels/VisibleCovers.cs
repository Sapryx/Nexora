namespace Nexora.ViewModels;

public class VisibleCovers
{
    public const int PrefetchRowCount = 1;
    private readonly double coverSize;
    private HashSet<TrackControlVm> shownTrackVms = [];

    public VisibleCovers(double coverSize)
    {
        this.coverSize = coverSize;
    }

    public void Show<TRow>(
        IReadOnlyList<TRow> rows,
        int firstRowIndex,
        int rowCount,
        Func<TRow, IEnumerable<TrackControlVm>> trackVmsOf)
    {
        var visibleTrackVms = rows.Skip(firstRowIndex).Take(rowCount).SelectMany(trackVmsOf).ToList();
        int prefetchCount = rowCount > 0 ? PrefetchRowCount : 0;
        var nearbyTrackVms = rows
            .Skip(Math.Max(0, firstRowIndex - prefetchCount))
            .Take(Math.Min(firstRowIndex, prefetchCount))
            .Concat(rows.Skip(firstRowIndex + rowCount).Take(prefetchCount))
            .SelectMany(trackVmsOf)
            .ToList();
        var newShownTrackVms = visibleTrackVms.Concat(nearbyTrackVms).ToHashSet();

        foreach(var trackVm in shownTrackVms.Where(it => !newShownTrackVms.Contains(it)))
        {
            trackVm.HideCover(coverSize);
        }

        visibleTrackVms.ForEach(trackVm => trackVm.ShowCover(coverSize, false));
        nearbyTrackVms.ForEach(trackVm => trackVm.ShowCover(coverSize, true));
        shownTrackVms = newShownTrackVms;
    }
}
