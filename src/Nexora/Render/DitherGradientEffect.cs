using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Nexora.Render;

public readonly record struct GradientStopSnapshot(Color Color, float Offset);

public class DitherGradientEffect : ICustomDrawOperation
{
    private const int MaxStops = 8;
    private static readonly SKRuntimeEffect? Effect;

    public Rect Bounds { get; }
    private readonly RelativePoint startPoint;
    private readonly RelativePoint endPoint;
    private readonly GradientStopSnapshot[] stops;

    static DitherGradientEffect()
    {
        var asset = AssetLoader.Open(new Uri("avares://Nexora/Assets/Shaders/DitherGradient.sksl"));
        using var reader = new StreamReader(asset);
        string shaderSource = reader.ReadToEnd();

        Effect = SKRuntimeEffect.CreateShader(shaderSource, out var errors);

        if(Effect == null)
        {
            throw new Exception($"SkSL shader compilation error: {errors}");
        }
    }

    public DitherGradientEffect(Rect bounds, RelativePoint startPoint, RelativePoint endPoint, GradientStopSnapshot[] stops)
    {
        Bounds = bounds;
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.stops = stops;
    }

    public void Dispose()
    {
    }

    private static SKPoint ToPixels(RelativePoint point, Size size)
    {
        return point.Unit == RelativeUnit.Relative
            ? new SKPoint((float)(point.Point.X * size.Width), (float)(point.Point.Y * size.Height))
            : new SKPoint((float)point.Point.X, (float)point.Point.Y);
    }

    public void Render(ImmediateDrawingContext context)
    {
        if(Effect is null || stops.Length < 2)
        {
            return;
        }

        if(context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } leaseFeature)
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        var size = Bounds.Size;
        var start = ToPixels(startPoint, size);
        var end = ToPixels(endPoint, size);

        var colors = new float[MaxStops * 4];
        var offsets = new float[MaxStops];

        for(int i = 0; i < MaxStops; i++)
        {
            var stop = i < stops.Length ? stops[i] : stops[^1];
            colors[i * 4 + 0] = stop.Color.R / 255f;
            colors[i * 4 + 1] = stop.Color.G / 255f;
            colors[i * 4 + 2] = stop.Color.B / 255f;
            colors[i * 4 + 3] = stop.Color.A / 255f;
            offsets[i] = i < stops.Length ? stop.Offset : 1.0f;
        }

        var builder = new SKRuntimeShaderBuilder(Effect);
        builder.Uniforms["u_size"] = new float[] { (float)size.Width, (float)size.Height };
        builder.Uniforms["u_start"] = new float[] { start.X, start.Y };
        builder.Uniforms["u_end"] = new float[] { end.X, end.Y };
        builder.Uniforms["u_colors"] = colors;
        builder.Uniforms["u_offsets"] = offsets;
        builder.Uniforms["u_stopCount"] = (float)stops.Length;

        using var shader = builder.Build();
        using var paint = new SKPaint();
        
        paint.Shader = shader;
        canvas.DrawRect(new SKRect(0, 0, (float)size.Width, (float)size.Height), paint);
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return other is DitherGradientEffect o && Bounds == o.Bounds;
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }
}
