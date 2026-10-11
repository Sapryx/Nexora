namespace Core.Search;

public class TrackMatch : IComparable<TrackMatch>
{
    public int Typos { get; }
    public MatchKind Kind { get; }
    public MatchField Field { get; }
    public int LayoutIndex { get; }

    public TrackMatch(int typos, MatchKind kind, MatchField field, int layoutIndex)
    {
        Typos = typos;
        Kind = kind;
        Field = field;
        LayoutIndex = layoutIndex;
    }

    public int CompareTo(TrackMatch? other)
    {
        if(other == null)
        {
            return 1;
        }

        int comparison = Typos.CompareTo(other.Typos);

        if(comparison == 0)
        {
            comparison = Kind.CompareTo(other.Kind);
        }

        if(comparison == 0)
        {
            comparison = Field.CompareTo(other.Field);
        }

        if(comparison == 0)
        {
            comparison = LayoutIndex.CompareTo(other.LayoutIndex);
        }

        return comparison;
    }
}
