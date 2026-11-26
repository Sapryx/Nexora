using Nexora.Render;

namespace Nexora.Media;

public class CoverMemoryBudget
{
    public const int Screens = 8;
    private readonly ICoverCache coverCache;
    private readonly IRenderScaleProvider renderScaleProvider;
    private readonly List<CoverViewport> viewports = [];

    public CoverMemoryBudget(ICoverCache coverCache, IRenderScaleProvider renderScaleProvider)
    {
        this.coverCache = coverCache;
        this.renderScaleProvider = renderScaleProvider;
    }

    public CoverViewport AddViewport()
    {
        var viewport = new CoverViewport(Update);
        viewports.Add(viewport);
        return viewport;
    }

    private void Update()
    {
        coverCache.MemoryBudget = Screens * viewports.Sum(viewport => viewport.CoverCount * BytesPerCover(viewport));
    }

    private long BytesPerCover(CoverViewport viewport)
    {
        long side = CoverSizes.ToPixels(viewport.CoverSize, renderScaleProvider.RenderScaling);
        return side * side * CoverSizes.BytesPerPixel;
    }
}
