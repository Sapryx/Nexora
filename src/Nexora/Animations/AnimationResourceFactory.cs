using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Transformation;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Animations;

public class AnimationResourceFactory
{
    private static readonly Dictionary<string, Func<Easing>> Easings = new Dictionary<string, Func<Easing>>()
    {
        ["LinearEasing"] = () => new LinearEasing(),
        ["SineEaseIn"] = () => new SineEaseIn(),
        ["SineEaseOut"] = () => new SineEaseOut(),
        ["SineEaseInOut"] = () => new SineEaseInOut(),
        ["QuadraticEaseIn"] = () => new QuadraticEaseIn(),
        ["QuadraticEaseOut"] = () => new QuadraticEaseOut(),
        ["QuadraticEaseInOut"] = () => new QuadraticEaseInOut(),
        ["CubicEaseIn"] = () => new CubicEaseIn(),
        ["CubicEaseOut"] = () => new CubicEaseOut(),
        ["CubicEaseInOut"] = () => new CubicEaseInOut(),
        ["QuarticEaseIn"] = () => new QuarticEaseIn(),
        ["QuarticEaseOut"] = () => new QuarticEaseOut(),
        ["QuarticEaseInOut"] = () => new QuarticEaseInOut(),
        ["QuinticEaseIn"] = () => new QuinticEaseIn(),
        ["QuinticEaseOut"] = () => new QuinticEaseOut(),
        ["QuinticEaseInOut"] = () => new QuinticEaseInOut(),
        ["ExponentialEaseIn"] = () => new ExponentialEaseIn(),
        ["ExponentialEaseOut"] = () => new ExponentialEaseOut(),
        ["ExponentialEaseInOut"] = () => new ExponentialEaseInOut(),
        ["CircularEaseIn"] = () => new CircularEaseIn(),
        ["CircularEaseOut"] = () => new CircularEaseOut(),
        ["CircularEaseInOut"] = () => new CircularEaseInOut(),
        ["BackEaseIn"] = () => new BackEaseIn(),
        ["BackEaseOut"] = () => new BackEaseOut(),
        ["BackEaseInOut"] = () => new BackEaseInOut(),
        ["ElasticEaseIn"] = () => new ElasticEaseIn(),
        ["ElasticEaseOut"] = () => new ElasticEaseOut(),
        ["ElasticEaseInOut"] = () => new ElasticEaseInOut(),
        ["BounceEaseIn"] = () => new BounceEaseIn(),
        ["BounceEaseOut"] = () => new BounceEaseOut(),
        ["BounceEaseInOut"] = () => new BounceEaseInOut()
    };
    private readonly ILogger logger;

    public AnimationResourceFactory(ILogger<AnimationResourceFactory> logger)
    {
        this.logger = logger;
    }

    public Dictionary<string, object> Create(AnimationSettings settings, AnimationSettings defaults)
    {
        return new Dictionary<string, object>()
        {
            ["HoverDuration"] = TimeSpan.FromMilliseconds(settings.Track.HoverDuration),
            ["TrackHoverBrush"] = CreateBrush(
                "track.hover_color",
                settings.Track.HoverColor,
                defaults.Track.HoverColor
            ),
            ["FadeDuration"] = TimeSpan.FromMilliseconds(settings.Track.Cover.HoverDuration),
            ["TrackCoverHoverOpacity"] = settings.Track.Cover.HoverOpacity,
            ["PopupOpenDuration"] = TimeSpan.FromMilliseconds(settings.Popup.OpenDuration),
            ["PopupCloseDuration"] = TimeSpan.FromMilliseconds(settings.Popup.CloseDuration),
            ["PopupClosedTransform"] = CreateScale(settings.Popup.InitialScale),
            ["PopupBackdropBrush"] = new ImmutableSolidColorBrush(Colors.Black, settings.Popup.BackdropOpacity),
            ["FlyoutOpenDuration"] = TimeSpan.FromMilliseconds(settings.ContextMenu.OpenDuration),
            ["SegmentSlideDuration"] = TimeSpan.FromMilliseconds(settings.SegmentedControl.SlideDuration),
            ["DefaultEaseIn"] = CreateEasing("default.ease_in", settings.Default.EaseIn, defaults.Default.EaseIn),
            ["DefaultEaseOut"] = CreateEasing("default.ease_out", settings.Default.EaseOut, defaults.Default.EaseOut)
        };
    }

    private static TransformOperations CreateScale(double scale)
    {
        var transform = TransformOperations.CreateBuilder(1);
        transform.AppendScale(scale, scale);
        return transform.Build();
    }

    private ImmutableSolidColorBrush CreateBrush(string key, string value, string fallback)
    {
        if(!Color.TryParse(value, out var color))
        {
            logger.Warn($"Ignored animation setting {key} '{value}': not a color, using {fallback}");
            color = Color.Parse(fallback);
        }

        return new ImmutableSolidColorBrush(color);
    }

    private Easing CreateEasing(string key, string value, string fallback)
    {
        if(Easings.TryGetValue(value, out var create))
        {
            return create();
        }

        logger.Warn($"Ignored animation setting {key} '{value}': unknown easing, using {fallback}");
        return Easings[fallback]();
    }
}
