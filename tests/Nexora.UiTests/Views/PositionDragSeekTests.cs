using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.Playback;
using Core.Playlists;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.ViewModels;
using Nexora.Views;
using Tests.Shared.Playback;

namespace Nexora.UiTests.Views;

public class PositionDragSeekTests
{
    private static readonly TimeSpan SeekInterval = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan TrackDuration = TimeSpan.FromSeconds(100);
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();

    [AvaloniaFact]
    public void DragPositionSlider_Fast_SeeksAtMostOncePerIntervalToLatestPosition()
    {
        using var app = new TestMainWindow(mediaEngine);
        var queue = app.Services.GetRequiredService<PlaybackQueue>();
        var playbackVm = app.Services.GetRequiredService<PlaybackVm>();
        var slider = PlaybackControlOf(app).FindControl<Slider>("PlaybackPositionSlider")
            ?? throw new InvalidOperationException("No position slider");
        queue.AddTrack(new AudioTrack("/music/one.opus", new Metadata() { Duration = TrackDuration }));
        queue.AddTrack(new AudioTrack("/music/two.opus", new Metadata() { Duration = TrackDuration }));
        Dispatcher.UIThread.RunJobs();
        app.Services.GetRequiredService<IAudioPlayer>().PlayTrack(queue.GetItem(0));
        Dispatcher.UIThread.RunJobs();

        mediaEngine.RaisePositionChanged(0.1f);
        Dispatcher.UIThread.RunJobs();
        Press(app, slider, 0.1);
        Move(app, slider, 0.3);
        Assert.Equal([(float)slider.Value], mediaEngine.SoughtPositions);
        float firstDragged = (float)slider.Value;
        Move(app, slider, 0.7);
        Move(app, slider, 0.2);
        Move(app, slider, 0.5);
        Assert.Equal([firstDragged], mediaEngine.SoughtPositions);

        mediaEngine.RaisePositionChanged(0.9f);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.5, slider.Value, 0.02);

        Advance(app, SeekInterval - TimeSpan.FromMilliseconds(1));
        Assert.Single(mediaEngine.SoughtPositions);
        Advance(app, TimeSpan.FromMilliseconds(1));
        Assert.Equal([firstDragged, (float)slider.Value], mediaEngine.SoughtPositions);

        Move(app, slider, 0.6);
        Assert.Equal(2, mediaEngine.SoughtPositions.Count);
        Move(app, slider, 0.8);
        Release(app, slider, 0.8);
        Assert.Equal(3, mediaEngine.SoughtPositions.Count);
        Assert.Equal((float)slider.Value, mediaEngine.SoughtPositions[^1]);
        Assert.Equal(0.8, slider.Value, 0.02);
        Advance(app, SeekInterval * 2);
        Assert.Equal(3, mediaEngine.SoughtPositions.Count);

        mediaEngine.RaisePositionChanged(0.81f);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.81, slider.Value, 4);

        Click(app, slider, 0.25);
        Assert.Equal(4, mediaEngine.SoughtPositions.Count);
        Assert.Equal((float)slider.Value, mediaEngine.SoughtPositions[^1]);
        Assert.Equal(0.25, slider.Value, 0.02);
        Advance(app, SeekInterval * 2);
        Assert.Equal(4, mediaEngine.SoughtPositions.Count);

        slider.Focus();
        PressKey(app, Key.Right);
        Assert.Equal(5, mediaEngine.SoughtPositions.Count);
        PressKey(app, Key.Right);
        Assert.Equal(6, mediaEngine.SoughtPositions.Count);
        PressKey(app, Key.Left);
        Assert.Equal(7, mediaEngine.SoughtPositions.Count);

        Press(app, slider, slider.Value);
        Move(app, slider, 0.4);
        Move(app, slider, 0.6);
        Assert.Equal(8, mediaEngine.SoughtPositions.Count);
        playbackVm.PlayNextTrack();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("/music/two.opus", mediaEngine.OpenedPath);
        Advance(app, SeekInterval);
        Move(app, slider, 0.7);
        Release(app, slider, 0.7);
        Advance(app, SeekInterval * 2);
        Assert.Equal(8, mediaEngine.SoughtPositions.Count);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
    }

    private static PlaybackControl PlaybackControlOf(TestMainWindow app)
    {
        return app.Window.GetVisualDescendants().OfType<PlaybackControl>().Single();
    }

    private static Point PointAt(TestMainWindow app, Slider slider, double fraction)
    {
        return slider.TranslatePoint(new Point(slider.Bounds.Width * fraction, slider.Bounds.Height / 2), app.Window)
            ?? throw new InvalidOperationException("Slider is not in the main window");
    }

    private static void Move(TestMainWindow app, Slider slider, double fraction)
    {
        app.Window.MouseMove(PointAt(app, slider, fraction));
        Dispatcher.UIThread.RunJobs();

        var playbackVm = app.Services.GetRequiredService<PlaybackVm>();
        var time = TrackDuration * fraction;
        Assert.Equal(fraction, slider.Value, 0.02);
        Assert.Equal($"{(int)time.TotalMinutes:00}:{time.Seconds:00}", playbackVm.SeekPreviewTime);
        Assert.True(ToolTip.GetIsOpen(slider));
    }

    private static void Press(TestMainWindow app, Slider slider, double fraction)
    {
        app.Window.MouseDown(PointAt(app, slider, fraction), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Release(TestMainWindow app, Slider slider, double fraction)
    {
        app.Window.MouseUp(PointAt(app, slider, fraction), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Click(TestMainWindow app, Slider slider, double fraction)
    {
        Press(app, slider, fraction);
        Release(app, slider, fraction);
    }

    private static void PressKey(TestMainWindow app, Key key)
    {
        var physicalKey = key == Key.Left ? PhysicalKey.ArrowLeft : PhysicalKey.ArrowRight;
        app.Window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
        app.Window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Advance(TestMainWindow app, TimeSpan time)
    {
        app.TimeProvider.Advance(time);
        Dispatcher.UIThread.RunJobs();
    }
}
