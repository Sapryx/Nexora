using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.Library;
using Core.Playback;
using Core.Playback.Loudness;
using Core.Storage;
using Core.Storage.Tags;
using Core.Tracks;
using DiscordRPC.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Nexora.Controls;
using Nexora.Media;
using Nexora.Render;
using Nexora.ViewModels;
using Nexora.Views;
using Tests.Shared.Integrations;
using Tests.Shared.Library;
using Tests.Shared.Playback;

namespace Nexora.UiTests;

public sealed class RecordingMainWindow : MainWindow
{
    public int MoveDragCount { get; private set; }
    public List<WindowEdge> ResizeDragEdges { get; } = [];

    protected override void StartMoveDrag(PointerPressedEventArgs e)
    {
        MoveDragCount++;
    }

    protected override void StartResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
    {
        ResizeDragEdges.Add(edge);
    }
}

public sealed class TestMainWindow : IDisposable
{
    public RecordingMainWindow Window { get; }
    public Mock<IAudioPlayer> AudioPlayerMock { get; } = Tests.Shared.Playback.AudioPlayerMock.Create();
    public FakeTimeProvider TimeProvider { get; } = new FakeTimeProvider();
    public FakeDiscordPipe DiscordPipe { get; } = new FakeDiscordPipe();
    public FakeTrackCoverLoader CoverLoader { get; } = new FakeTrackCoverLoader();
    public TestRenderScale RenderScale { get; } = new TestRenderScale();
    public string ThumbnailDirectory { get; }
    public IServiceProvider Services => provider;
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);
    private readonly ServiceProvider provider;
    private readonly string screenshotDirectory = Path.Combine(ProjectDirectory(), "screenshots");
    private readonly string screenshotPath;
    private readonly bool ownsThumbnailDirectory;
    private readonly List<IAudioTrack> loadedTracks = [];

    public TestMainWindow([CallerMemberName] string testName = "") : this(null, null, null, testName)
    {
    }

    public TestMainWindow(
        FakeMediaEngine? mediaEngine,
        IMetadataSaver? metadataSaver = null,
        string? thumbnailDirectory = null,
        [CallerMemberName] string testName = "",
        string? musicDirectory = null)
    {
        screenshotPath = Path.Combine(screenshotDirectory, $"{testName}.png");
        ownsThumbnailDirectory = thumbnailDirectory == null;
        ThumbnailDirectory = thumbnailDirectory ?? CreateTemporaryDirectory();

        var services = new ServiceCollection();
        services.AddLogging();
        CompositionRoot.Configure(services);

        if(mediaEngine == null)
        {
            services.AddSingleton(AudioPlayerMock.Object);
        }
        else
        {
            services.AddSingleton<IMediaEngine>(mediaEngine);
            services.AddSingleton(Mock.Of<IVolumeStorage>());
        }

        if(metadataSaver != null)
        {
            services.AddSingleton(metadataSaver);
        }

        services.AddSingleton(Mock.Of<ILoudnessAnalyzer>());

        if(musicDirectory == null)
        {
            services.AddSingleton(TestMusicLibrary.CreateTrackLoader(loadedTracks));
        }
        else
        {
            services.AddSingleton<IMusicDirectoryProvider>(serviceProvider => new MusicDirectoryProvider(
                serviceProvider.GetRequiredService<ILogger<MusicDirectoryProvider>>(),
                musicDirectory
            ));
        }

        services.AddSingleton<TimeProvider>(TimeProvider);
        services.AddSingleton<INamedPipeClient>(DiscordPipe);
        services.AddSingleton<ITrackCoverLoader>(CoverLoader);
        services.AddSingleton<IRenderScaleProvider>(RenderScale);
        services.AddSingleton<IThumbnailStore>(serviceProvider => new ThumbnailStore(
            ThumbnailDirectory,
            serviceProvider.GetRequiredService<ILogger<ThumbnailStore>>()
        ));
        provider = services.BuildServiceProvider();

        Window = new RecordingMainWindow()
        {
            Width = 1280,
            Height = 800,
            DataContext = provider.GetRequiredService<MainWindowVm>()
        };
        Window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Hover(Control control)
    {
        Window.MouseMove(CenterOf(control));
        Dispatcher.UIThread.RunJobs();
    }

    public void Click(Control control)
    {
        Click(CenterOf(control));
    }

    public void Click(Point point)
    {
        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    public void PressKey(Key key, PhysicalKey physicalKey)
    {
        Window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
        Window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
        Dispatcher.UIThread.RunJobs();
    }

    public T Find<T>() where T : Control
    {
        return Window.GetVisualDescendants().OfType<T>().Single();
    }

    public T Find<T>(string name) where T : Control
    {
        return Window.GetVisualDescendants().OfType<T>().Single(it => it.Name == name);
    }

    public Button FindButton(string iconName, Visual? container = null)
    {
        return (container ?? Window).GetVisualDescendants().OfType<Button>()
            .Single(button => button.GetVisualDescendants().OfType<Icon>().Any(icon => icon.IconName == iconName));
    }

    public void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + WaitTimeout;

        while(!condition())
        {
            if(DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met in time");
            }

            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
    }

    public Point PointIn(Control control, double fractionX, double fractionY)
    {
        var point = new Point(control.Bounds.Width * fractionX, control.Bounds.Height * fractionY);
        return control.TranslatePoint(point, Window)
            ?? throw new InvalidOperationException($"{control} is not in the main window");
    }

    public void AddToLibrary(IEnumerable<IAudioTrack> tracks)
    {
        Services.GetRequiredService<MusicLibrary>().Playlist.AddTracks(tracks);
        Dispatcher.UIThread.RunJobs();
    }

    public void ShowLibraryGrid()
    {
        Find<SegmentedControl>("LibraryViewSwitch").SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
    }

    public void ShowLibraryList()
    {
        Find<SegmentedControl>("LibraryViewSwitch").SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
    }

    public List<AudioTrack> AddLibraryWithCovers(int count)
    {
        var tracks = CreateTracksWithCovers(count);
        AddToLibrary(tracks);
        return tracks;
    }

    public List<AudioTrack> CreateTracksWithCovers(int count)
    {
        CoverLoader.AddToEveryTrack(new PixelSize(200, 200));
        return Enumerable.Range(0, count)
            .Select(index => new AudioTrack(
                $"/music/Track {index:D5}.mp3",
                new Metadata() { Title = $"Track {index:D5}", Duration = TimeSpan.FromSeconds(100) }
            ))
            .ToList();
    }

    public ThumbnailGenerator LoadLibrary(IEnumerable<IAudioTrack> tracks)
    {
        loadedTracks.AddRange(tracks);
        var generator = Services.GetRequiredService<ThumbnailGenerator>();
        generator.Start();
        Services.GetRequiredService<MusicLibrary>().LoadAsync().Wait();
        Dispatcher.UIThread.RunJobs();
        return generator;
    }

    public ScrollViewer LibraryGridScroll()
    {
        return Find<ScrollViewer>("RowScrollViewer");
    }

    public ScrollViewer LibraryListScroll()
    {
        return TrackAreaScroll(Find<TrackArea>("LibraryList"));
    }

    public ScrollViewer TrackAreaScroll(TrackArea trackArea)
    {
        return trackArea.GetVisualDescendants().OfType<ScrollViewer>().First();
    }

    public T FindTrack<T>(string audioPath) where T : Control
    {
        return Window.GetVisualDescendants().OfType<T>()
            .Single(it => it.IsEffectivelyVisible && (it.DataContext as TrackControlVm)?.AudioPath == audioPath);
    }

    public Image CoverImageOf(Control track)
    {
        return track.GetVisualDescendants().OfType<Image>().First();
    }

    public List<Image> VisibleCoverImages<T>(ScrollViewer scrollViewer) where T : Control
    {
        return scrollViewer.GetVisualDescendants().OfType<T>()
            .Where(track => track.IsEffectivelyVisible && IsInViewport(track, scrollViewer))
            .Select(CoverImageOf)
            .ToList();
    }

    public void PassCoverShowDelay()
    {
        TimeProvider.Advance(Services.GetRequiredService<CoverLoadQueue>().ShowDelay);
        Dispatcher.UIThread.RunJobs();
    }

    public List<string> VisibleTrackPaths<T>(ScrollViewer scrollViewer) where T : Control
    {
        return VisibleCoverImages<T>(scrollViewer)
            .Select(image => image.DataContext)
            .OfType<TrackControlVm>()
            .Select(trackVm => trackVm.AudioPath)
            .ToList();
    }

    public static List<string> NearbyRowPaths(
        List<AudioTrack> tracks,
        IReadOnlyCollection<string> visiblePaths,
        int columnCount)
    {
        int firstRow = tracks.FindIndex(track => visiblePaths.Contains(track.AudioPath)) / columnCount;
        int lastRow = tracks.FindLastIndex(track => visiblePaths.Contains(track.AudioPath)) / columnCount;
        int firstRowStart = firstRow * columnCount;
        int lastRowEnd = (lastRow + 1) * columnCount;
        return tracks
            .Where((_, index) => index >= firstRowStart - columnCount && index < firstRowStart
                || index >= lastRowEnd && index < lastRowEnd + columnCount)
            .Select(track => track.AudioPath)
            .ToList();
    }

    public void WaitForVisibleCovers<T>(ScrollViewer scrollViewer) where T : Control
    {
        WaitUntil(() =>
        {
            PassCoverShowDelay();
            var images = VisibleCoverImages<T>(scrollViewer);
            return images.Count > 0 && images.All(image => image.Source != null);
        });
    }

    public void ScrollTo(ScrollViewer scrollViewer, double offset)
    {
        scrollViewer.Offset = new Vector(0, offset);
        Dispatcher.UIThread.RunJobs();
    }

    public void ScrollByScreens(ScrollViewer scrollViewer, double screens)
    {
        double maxOffset = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
        double offset = scrollViewer.Offset.Y + screens * scrollViewer.Viewport.Height;
        ScrollTo(scrollViewer, Math.Clamp(offset, 0, maxOffset));
    }

    private static bool IsInViewport(Control control, ScrollViewer scrollViewer)
    {
        var top = control.TranslatePoint(new Point(0, 0), scrollViewer);
        return top != null
            && top.Value.Y < scrollViewer.Viewport.Height
            && top.Value.Y + control.Bounds.Height > 0;
    }

    public static string CreateTemporaryDirectory()
    {
        return Directory.CreateTempSubdirectory("nexora-").FullName;
    }

    public static PixelSize ThumbnailSize(string filePath)
    {
        using var bitmap = new Bitmap(filePath);
        return bitmap.PixelSize;
    }

    private static string ProjectDirectory([CallerFilePath] string sourcePath = "")
    {
        return Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException($"{sourcePath} has no directory");
    }

    private Point CenterOf(Control control)
    {
        return PointIn(control, 0.5, 0.5);
    }

    public void Dispose()
    {
        if(Window.IsVisible)
        {
            Directory.CreateDirectory(screenshotDirectory);
            Window.CaptureRenderedFrame()?.Save(screenshotPath, PngBitmapEncoderOptions.Default);
            Window.Close();
        }

        provider.Dispose();

        if(ownsThumbnailDirectory && Directory.Exists(ThumbnailDirectory))
        {
            Directory.Delete(ThumbnailDirectory, true);
        }
    }
}
