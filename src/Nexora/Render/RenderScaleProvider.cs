using Avalonia.Controls;

namespace Nexora.Render;

public class RenderScaleProvider
{
    public double RenderScaling { get; private set; } = 1;

    public void Track(TopLevel topLevel)
    {
        RenderScaling = topLevel.RenderScaling;
        topLevel.ScalingChanged += (_, _) => RenderScaling = topLevel.RenderScaling;
    }
}
