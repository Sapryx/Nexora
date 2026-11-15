using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Nexora.Views;

public static class ResizeEdges
{
    private static readonly WindowEdge?[,] Edges = new WindowEdge?[,]
    {
        {
            WindowEdge.NorthWest,
            WindowEdge.North,
            WindowEdge.NorthEast
        },
        {
            WindowEdge.West,
            null,
            WindowEdge.East
        },
        {
            WindowEdge.SouthWest,
            WindowEdge.South,
            WindowEdge.SouthEast
        }
    };

    private static readonly Dictionary<WindowEdge, StandardCursorType> Cursors =
        new Dictionary<WindowEdge, StandardCursorType>()
        {
            {
                WindowEdge.NorthWest, StandardCursorType.TopLeftCorner
            },
            {
                WindowEdge.North, StandardCursorType.TopSide
            },
            {
                WindowEdge.NorthEast, StandardCursorType.TopRightCorner
            },
            {
                WindowEdge.West, StandardCursorType.LeftSide
            },
            {
                WindowEdge.East, StandardCursorType.RightSide
            },
            {
                WindowEdge.SouthWest, StandardCursorType.BottomLeftCorner
            },
            {
                WindowEdge.South, StandardCursorType.BottomSide
            },
            {
                WindowEdge.SouthEast, StandardCursorType.BottomRightCorner
            }
        };

    public static WindowEdge? Find(Point position, Size size, double thickness)
    {
        return Edges[Band(position.Y, size.Height, thickness), Band(position.X, size.Width, thickness)];
    }

    public static StandardCursorType CursorOf(WindowEdge edge)
    {
        return Cursors[edge];
    }

    private static int Band(double coordinate, double length, double thickness)
    {
        if(coordinate < thickness)
        {
            return 0;
        }

        return coordinate >= length - thickness ? 2 : 1;
    }
}
