using Nexora.Render;

namespace Nexora.UiTests;

public sealed class TestRenderScale : IRenderScaleProvider
{
    public double RenderScaling { get; set; } = 1;
}
