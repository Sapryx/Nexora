using System.Numerics;

namespace Nexora.Render;

public class DragSample
{
    public required double StartTime { get; init; }
    public required double Time { get; init; }
    public required Vector2 Delta { get; init; }
}
