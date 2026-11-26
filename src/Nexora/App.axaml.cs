using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Nexora;

public partial class App : Application
{
    public required Action LoadResources { get; init; }
    public required Func<Window> Start { get; init; }

    public override void Initialize()
    {
        LoadResources();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
