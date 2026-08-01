using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Nexora.Render;

public class DitheringControl : Control
{
    public static readonly StyledProperty<RelativePoint> StartPointProperty =
        AvaloniaProperty.Register<DitheringControl, RelativePoint>(nameof(StartPoint), RelativePoint.TopLeft
    );

    public static readonly StyledProperty<RelativePoint> EndPointProperty =
        AvaloniaProperty.Register<DitheringControl, RelativePoint>(nameof(EndPoint), RelativePoint.BottomRight
    );

    public static readonly StyledProperty<GradientStops> GradientStopsProperty = 
        AvaloniaProperty.Register<DitheringControl, GradientStops>(nameof(GradientStops)
    );

    public RelativePoint StartPoint
    {
        get => GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public RelativePoint EndPoint
    {
        get => GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    [Content]
    public GradientStops GradientStops
    {
        get => GetValue(GradientStopsProperty);
        set => SetValue(GradientStopsProperty, value);
    }

    static DitheringControl()
    {
        AffectsRender<DitheringControl>(StartPointProperty, EndPointProperty, GradientStopsProperty);
    }

    public DitheringControl()
    {
        GradientStops = [];
        GradientStops.CollectionChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if(GradientStops.Count < 2)
        {
            return;
        }
        
        const int ShaderMaxStops = 8;
        
        // Data is copied here because rendering is asynchronous further down the line,
        // so you can't access any UI thread objects from there.
        var snapshot = GradientStops
            .OrderBy(stop => stop.Offset)
            .Take(ShaderMaxStops)
            .Select(stop => new GradientStopSnapshot(stop.Color, (float)stop.Offset))
            .ToArray();
        
        context.Custom(new DitherGradientEffect(Bounds, StartPoint, EndPoint, snapshot));
    }
}
