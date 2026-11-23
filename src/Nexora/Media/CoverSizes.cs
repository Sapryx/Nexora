namespace Nexora.Media;

public static class CoverSizes
{
    public const double Thumbnail = 64;
    public const double Tile = 160;
    public const int BytesPerPixel = 4;

    public static int ToPixels(double size, double renderScaling)
    {
        return (int)Math.Ceiling(size * renderScaling);
    }
}
