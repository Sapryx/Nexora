using Avalonia;
using Avalonia.Headless;
using Nexora.UiTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Nexora.UiTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions() { UseHeadlessDrawing = false })
            .WithInterFont();
    }
}
