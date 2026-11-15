using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Nexora.Views;

namespace Nexora.Tests.Views;

public class ResizeEdgesTests
{
    private static readonly Size WindowSize = new Size(1000, 800);

    [Theory]
    [InlineData(2, 2, WindowEdge.NorthWest)]
    [InlineData(500, 2, WindowEdge.North)]
    [InlineData(998, 2, WindowEdge.NorthEast)]
    [InlineData(2, 400, WindowEdge.West)]
    [InlineData(998, 400, WindowEdge.East)]
    [InlineData(2, 798, WindowEdge.SouthWest)]
    [InlineData(500, 798, WindowEdge.South)]
    [InlineData(998, 798, WindowEdge.SouthEast)]
    public void Find_PointInBorder_ReturnsEdge(double x, double y, WindowEdge expected)
    {
        Assert.Equal(expected, ResizeEdges.Find(new Point(x, y), WindowSize, 6));
    }

    [Theory]
    [InlineData(500, 400)]
    [InlineData(6, 6)]
    [InlineData(993, 793)]
    public void Find_PointInsideBorder_ReturnsNull(double x, double y)
    {
        Assert.Null(ResizeEdges.Find(new Point(x, y), WindowSize, 6));
    }

    [Theory]
    [InlineData(WindowEdge.NorthWest, StandardCursorType.TopLeftCorner)]
    [InlineData(WindowEdge.North, StandardCursorType.TopSide)]
    [InlineData(WindowEdge.NorthEast, StandardCursorType.TopRightCorner)]
    [InlineData(WindowEdge.West, StandardCursorType.LeftSide)]
    [InlineData(WindowEdge.East, StandardCursorType.RightSide)]
    [InlineData(WindowEdge.SouthWest, StandardCursorType.BottomLeftCorner)]
    [InlineData(WindowEdge.South, StandardCursorType.BottomSide)]
    [InlineData(WindowEdge.SouthEast, StandardCursorType.BottomRightCorner)]
    public void CursorOf_Edge_ReturnsMatchingCursor(WindowEdge edge, StandardCursorType expected)
    {
        Assert.Equal(expected, ResizeEdges.CursorOf(edge));
    }
}
