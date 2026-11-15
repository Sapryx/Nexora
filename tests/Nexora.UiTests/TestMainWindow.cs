using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Core.Playback;
using Core.Playback.Loudness;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexora.ViewModels;
using Nexora.Views;
using Tests.Shared.Playback;

namespace Nexora.UiTests;

public sealed class TestMainWindow : IDisposable
{
    public MainWindow Window { get; }
    public Mock<IAudioPlayer> AudioPlayerMock { get; } = Tests.Shared.Playback.AudioPlayerMock.Create();
    private readonly ServiceProvider provider;
    private readonly string screenshotDirectory = Path.Combine(AppContext.BaseDirectory, "screenshots");
    private readonly string screenshotPath;

    public TestMainWindow([CallerMemberName] string testName = "")
    {
        screenshotPath = Path.Combine(screenshotDirectory, $"{testName}.png");

        var services = new ServiceCollection();
        services.AddLogging();
        CompositionRoot.Configure(services);
        services.AddSingleton(AudioPlayerMock.Object);
        services.AddSingleton(Mock.Of<ILoudnessAnalyzer>());
        provider = services.BuildServiceProvider();

        Window = new MainWindow()
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
        var point = CenterOf(control);

        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private Point CenterOf(Control control)
    {
        var center = new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
        return control.TranslatePoint(center, Window)
            ?? throw new InvalidOperationException($"{control} is not in the main window");
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
    }
}
