using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Animations;
using Tests.Shared.Logging;

namespace Nexora.Tests.Animations;

public class AnimationResourceFactoryTests
{
    private readonly TestLogger<AnimationResourceFactory> logger = new TestLogger<AnimationResourceFactory>();
    private readonly AnimationResourceFactory factory;
    private readonly AnimationSettingsLoader loader;

    public AnimationResourceFactoryTests()
    {
        factory = new AnimationResourceFactory(logger);
        loader = new AnimationSettingsLoader(
            AnimationSettingsLoaderTests.ReadDefaults(),
            null,
            NullLogger<AnimationSettingsLoader>.Instance
        );
    }

    [Fact]
    public void Create_Defaults_ReturnsDefaultResources()
    {
        var resources = factory.Create(loader.LoadDefaults(), loader.LoadDefaults());

        Assert.Equal(TimeSpan.FromMilliseconds(100), resources["HoverDuration"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), resources["FadeDuration"]);
        Assert.Equal(TimeSpan.FromMilliseconds(150), resources["PopupOpenDuration"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), resources["PopupCloseDuration"]);
        Assert.Equal(TimeSpan.FromMilliseconds(120), resources["FlyoutOpenDuration"]);
        Assert.Equal(TimeSpan.FromMilliseconds(200), resources["SegmentSlideDuration"]);
        Assert.Equal(0.5, resources["TrackCoverHoverOpacity"]);
        Assert.Equal(Color.Parse("#23FFFFFF"), ((ISolidColorBrush)resources["TrackHoverBrush"]).Color);
        Assert.IsType<CubicEaseIn>(resources["DefaultEaseIn"]);
        Assert.IsType<CubicEaseOut>(resources["DefaultEaseOut"]);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Create_InitialScale_ReturnsScaleTransform()
    {
        var settings = loader.LoadDefaults();
        settings.Popup.InitialScale = 0.8;

        var resources = factory.Create(settings, loader.LoadDefaults());

        var operation = Assert.Single(((TransformOperations)resources["PopupClosedTransform"]).Operations);
        Assert.Equal(TransformOperation.OperationType.Scale, operation.Type);
        Assert.Equal(0.8, operation.Data.Scale.X);
        Assert.Equal(0.8, operation.Data.Scale.Y);
    }

    [Fact]
    public void Create_KnownEasing_ReturnsEasingOfThatType()
    {
        var settings = loader.LoadDefaults();
        settings.Default.EaseOut = "BounceEaseOut";

        var resources = factory.Create(settings, loader.LoadDefaults());

        Assert.IsType<BounceEaseOut>(resources["DefaultEaseOut"]);
    }

    [Fact]
    public void Create_UnknownEasing_LogsWarningAndUsesDefault()
    {
        var settings = loader.LoadDefaults();
        settings.Default.EaseIn = "Wobbly";

        var resources = factory.Create(settings, loader.LoadDefaults());

        Assert.IsType<CubicEaseIn>(resources["DefaultEaseIn"]);
        Assert.Contains(
            logger.Entries,
            it => it.Level == LogLevel.Warning && it.Message.Contains("default.ease_in")
        );
    }

    [Fact]
    public void Create_InvalidColor_LogsWarningAndUsesDefault()
    {
        var settings = loader.LoadDefaults();
        settings.Track.HoverColor = "not a color";

        var resources = factory.Create(settings, loader.LoadDefaults());

        Assert.Equal(Color.Parse("#23FFFFFF"), ((ISolidColorBrush)resources["TrackHoverBrush"]).Color);
        Assert.Contains(
            logger.Entries,
            it => it.Level == LogLevel.Warning && it.Message.Contains("track.hover_color")
        );
    }
}
