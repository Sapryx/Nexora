namespace Nexora.Media;

public sealed class CoverViewport
{
    public int CoverCount { get; private set; }
    public double CoverSize { get; private set; }
    private readonly Action changed;

    public CoverViewport(Action changed)
    {
        this.changed = changed;
    }

    public void Show(int coverCount, double coverSize)
    {
        if(coverCount == CoverCount && coverSize == CoverSize)
        {
            return;
        }

        CoverCount = coverCount;
        CoverSize = coverSize;
        changed();
    }
}
