using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.Library;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Controls;
using Nexora.ViewModels;
using Tests.Shared.Playback;

namespace Nexora.UiTests.Views;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Nexora/Media")]
[Trait("Covers", "src/Nexora/ViewModels/CoveredTrackVm.cs")]
[Trait("Covers", "src/Nexora/Controls/TrackControl.axaml")]
[Trait("Covers", "src/Nexora/Controls/TrackTile.axaml")]
[Trait("Covers", "src/Nexora/Controls/TrackView.axaml")]
public class TrackCoverSizeTests
{
    private const string SquarePath = "/music/Square.mp3";
    private const string WidePath = "/music/Wide.mp3";
    private const string TallPath = "/music/Tall.mp3";
    private const string NoCoverPath = "/music/NoCover.mp3";
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();

    [AvaloniaFact]
    public void Covers_AtDefaultScale_AreLoadedAtTheSizeTheyAreShownAt()
    {
        using var app = new TestMainWindow(mediaEngine);
        AddLibrary(app);

        WaitForCovers<TrackTile>(app);
        AssertCover<TrackTile>(app, SquarePath, new PixelSize(160, 160), new Size(160, 160));
        AssertCover<TrackTile>(app, WidePath, new PixelSize(160, 80), new Size(160, 80));
        AssertCover<TrackTile>(app, TallPath, new PixelSize(80, 160), new Size(80, 160));
        Assert.Null(CoverImage<TrackTile>(app, NoCoverPath).Source);

        ShowList(app);
        WaitForCovers<TrackControl>(app);
        AssertCover<TrackControl>(app, SquarePath, new PixelSize(64, 64), new Size(64, 64));
        AssertCover<TrackControl>(app, WidePath, new PixelSize(64, 32), new Size(64, 32));
        AssertCover<TrackControl>(app, TallPath, new PixelSize(32, 64), new Size(32, 64));
        Assert.Null(CoverImage<TrackControl>(app, NoCoverPath).Source);

        app.Click(PlayButton(app, SquarePath));
        var playerImage = app.Find<TrackView>().GetVisualDescendants().OfType<Image>().Single();
        app.WaitUntil(() => playerImage.Source != null);
        Assert.Equal(new PixelSize(64, 64), PixelSizeOf(playerImage));
        Assert.Equal(new Size(64, 64), playerImage.Bounds.Size);

        app.Click(app.Find<TrackView>().GetVisualDescendants().OfType<Button>().First());
        var coverViewerVm = app.Services.GetRequiredService<CoverViewerVm>();
        Assert.True(coverViewerVm.IsOpen);
        app.WaitUntil(() => coverViewerVm.LargeCover.Cover?.PixelSize == new PixelSize(500, 500));
        app.Services.GetRequiredService<MainWindowVm>().ClosePopup();

        TrackVm(app, SquarePath).Edit();
        var trackEditorVm = app.Services.GetRequiredService<TrackEditorVm>();
        app.WaitUntil(() => trackEditorVm.LargeCover.Cover?.PixelSize == new PixelSize(500, 500));
        app.Services.GetRequiredService<MainWindowVm>().ClosePopup();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Covers_At150PercentScale_AreLoadedAtTheScaledSize()
    {
        using var app = new TestMainWindow(mediaEngine);
        app.RenderScale.RenderScaling = 1.5;
        AddLibrary(app);

        WaitForCovers<TrackTile>(app);
        Assert.Equal(new PixelSize(240, 240), PixelSizeOf(CoverImage<TrackTile>(app, SquarePath)));
        Assert.Equal(new PixelSize(240, 120), PixelSizeOf(CoverImage<TrackTile>(app, WidePath)));

        ShowList(app);
        WaitForCovers<TrackControl>(app);
        Assert.Equal(new PixelSize(96, 96), PixelSizeOf(CoverImage<TrackControl>(app, SquarePath)));
        Assert.Equal(new PixelSize(96, 48), PixelSizeOf(CoverImage<TrackControl>(app, WidePath)));

        app.Click(PlayButton(app, SquarePath));
        var playerImage = app.Find<TrackView>().GetVisualDescendants().OfType<Image>().Single();
        app.WaitUntil(() => playerImage.Source != null);
        Assert.Equal(new PixelSize(96, 96), PixelSizeOf(playerImage));
    }

    private static void AddLibrary(TestMainWindow app)
    {
        app.CoverLoader.Add(SquarePath, new PixelSize(500, 500));
        app.CoverLoader.Add(WidePath, new PixelSize(600, 300));
        app.CoverLoader.Add(TallPath, new PixelSize(300, 600));
        app.Services.GetRequiredService<MusicLibrary>().Playlist.AddTracks([
            CreateTrack(SquarePath, "Square"),
            CreateTrack(WidePath, "Wide"),
            CreateTrack(TallPath, "Tall"),
            CreateTrack(NoCoverPath, "No Cover")
        ]);
        Dispatcher.UIThread.RunJobs();
    }

    private static AudioTrack CreateTrack(string audioPath, string title)
    {
        return new AudioTrack(audioPath, new Metadata() { Title = title, Duration = TimeSpan.FromSeconds(100) });
    }

    private static void ShowList(TestMainWindow app)
    {
        app.Find<SegmentedControl>("LibraryViewSwitch").SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
    }

    private static void WaitForCovers<T>(TestMainWindow app) where T : Control
    {
        string[] coveredPaths = [SquarePath, WidePath, TallPath];
        app.WaitUntil(() => coveredPaths.All(path => CoverImage<T>(app, path).Source != null));
    }

    private static void AssertCover<T>(TestMainWindow app, string audioPath, PixelSize pixelSize, Size size)
        where T : Control
    {
        var image = CoverImage<T>(app, audioPath);
        Assert.Equal(pixelSize, PixelSizeOf(image));
        Assert.Equal(size, image.Bounds.Size);
    }

    private static PixelSize PixelSizeOf(Image image)
    {
        var bitmap = Assert.IsType<Bitmap>(image.Source);
        return bitmap.PixelSize;
    }

    private static Image CoverImage<T>(TestMainWindow app, string audioPath) where T : Control
    {
        return FindTrack<T>(app, audioPath).GetVisualDescendants().OfType<Image>().First();
    }

    private static T FindTrack<T>(TestMainWindow app, string audioPath) where T : Control
    {
        return app.Window.GetVisualDescendants().OfType<T>()
            .Single(it => it.IsEffectivelyVisible && (it.DataContext as TrackControlVm)?.AudioPath == audioPath);
    }

    private static Button PlayButton(TestMainWindow app, string audioPath)
    {
        return FindTrack<TrackControl>(app, audioPath).GetVisualDescendants().OfType<Button>()
            .Single(it => it.Name == "TogglePlayButton");
    }

    private static TrackControlVm TrackVm(TestMainWindow app, string audioPath)
    {
        var vm = FindTrack<TrackControl>(app, audioPath).DataContext;
        return Assert.IsType<TrackControlVm>(vm);
    }
}
