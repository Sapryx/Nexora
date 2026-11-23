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
using Nexora.Controls;
using Nexora.ViewModels;
using Nexora.Views;
using Tests.Shared.Playback;

namespace Nexora.UiTests.Views;

public class PlaybackEndSeekTests
{
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();

    [AvaloniaFact]
    public void Seek_PausedAtEndOfLastTrack_IsKeptAndPlayResumesFromIt()
    {
        using var app = new TestMainWindow(mediaEngine);
        var queue = app.Services.GetRequiredService<PlaybackQueue>();
        var playbackVm = app.Services.GetRequiredService<PlaybackVm>();
        var slider = app.Find<Slider>("PlaybackPositionSlider");
        queue.AddTrack(new AudioTrack("/music/one.opus", new Metadata() { Duration = TimeSpan.FromSeconds(100) }));
        app.Services.GetRequiredService<IAudioPlayer>().PlayTrack(queue.GetItem(0));
        Dispatcher.UIThread.RunJobs();

        Finish(app);
        Assert.False(playbackVm.IsPlaying);
        Assert.NotNull(playbackVm.PlayingTrackViewVm);

        app.Click(app.FindButton("play", app.Find<PlaybackControl>()));
        Assert.True(playbackVm.IsPlaying);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);

        Finish(app);
        DragToMiddle(app, slider);
        Assert.InRange(slider.Value, 0.49, 0.51);
        Assert.False(playbackVm.IsPlaying);
        Assert.Equal(2, mediaEngine.OpenCount);

        app.Click(app.FindButton("play", app.Find<PlaybackControl>()));
        Assert.True(playbackVm.IsPlaying);
        Assert.InRange(mediaEngine.OpenedStartTimeMs, 49000, 51000);

        Finish(app);
        app.PressKey(Key.Left, PhysicalKey.ArrowLeft);
        Assert.Equal(0.95, slider.Value, 4);
        Assert.False(playbackVm.IsPlaying);

        app.Click(app.FindButton("play", app.Find<PlaybackControl>()));
        Assert.True(playbackVm.IsPlaying);
        Assert.Equal(95000, mediaEngine.OpenedStartTimeMs);

        Finish(app);
        DragToMiddle(app, slider);
        queue.AddTrack(new AudioTrack("/music/two.opus", new Metadata() { Duration = TimeSpan.FromSeconds(100) }));
        Dispatcher.UIThread.RunJobs();
        app.Services.GetRequiredService<IAudioPlayer>().PlayTrack(queue.GetItem(1));
        Dispatcher.UIThread.RunJobs();
        Assert.True(playbackVm.IsPlaying);
        Assert.Equal("/music/two.opus", mediaEngine.OpenedPath);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.Equal(0, slider.Value);
        double?[] queueProgress = [0, 0];
        Assert.Equal(
            queueProgress,
            app.Window.GetVisualDescendants().OfType<TrackControl>()
                .Select(control => (control.DataContext as TrackControlVm)?.Progress)
        );
    }

    private static void DragToMiddle(TestMainWindow app, Slider slider)
    {
        var start = app.PointIn(slider, 0.98, 0.5);
        var middle = app.PointIn(slider, 0.5, 0.5);

        app.Window.MouseDown(start, MouseButton.Left);
        app.Window.MouseMove(middle);
        app.Window.MouseUp(middle, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private void Finish(TestMainWindow app)
    {
        mediaEngine.RaisePositionChanged(0.99f);
        mediaEngine.RaiseEndReached();
        Dispatcher.UIThread.RunJobs();
    }
}
