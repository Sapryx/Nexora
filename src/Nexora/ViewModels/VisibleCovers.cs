namespace Nexora.ViewModels;

public class VisibleCovers
{
    private readonly double coverSize;
    private HashSet<TrackControlVm> shownTrackVms = [];

    public VisibleCovers(double coverSize)
    {
        this.coverSize = coverSize;
    }

    public void Show(IEnumerable<TrackControlVm> trackVms)
    {
        var visibleTrackVms = trackVms.ToHashSet();

        foreach(var trackVm in shownTrackVms.Where(it => !visibleTrackVms.Contains(it)))
        {
            trackVm.HideCover(coverSize);
        }

        foreach(var trackVm in visibleTrackVms.Where(it => !shownTrackVms.Contains(it)))
        {
            trackVm.ShowCover(coverSize);
        }

        shownTrackVms = visibleTrackVms;
    }
}
