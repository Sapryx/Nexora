
namespace Nexora.ViewModels;

public class TrackGridRow
{
    public IReadOnlyList<TrackControlVm> TrackVms { get; }

    public TrackGridRow(IReadOnlyList<TrackControlVm> trackVms)
    {
        TrackVms = trackVms;
    }

    /// <summary>
    /// Splits <paramref name="trackVms"/> into rows of <paramref name="columnCount"/> items, replacing only the rows
    /// that changed, so the list keeps its scroll position and the containers of unchanged rows.
    /// </summary>
    public static void UpdateRows(IList<TrackGridRow> rows, IReadOnlyList<TrackControlVm> trackVms, int columnCount)
    {
        int rowCount = (trackVms.Count + columnCount - 1) / columnCount;

        for(int index = 0; index < rowCount; index++)
        {
            var rowTrackVms = trackVms.Skip(index * columnCount).Take(columnCount).ToList();

            if(index == rows.Count)
            {
                rows.Add(new TrackGridRow(rowTrackVms));
            }
            else if(!rows[index].TrackVms.SequenceEqual(rowTrackVms))
            {
                rows[index] = new TrackGridRow(rowTrackVms);
            }
        }

        while(rows.Count > rowCount)
        {
            rows.RemoveAt(rows.Count - 1);
        }
    }
}
