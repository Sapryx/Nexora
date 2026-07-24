using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core;
using Gui.ViewModels;
using Gui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Gui;

public partial class App : Application
{
    private static ServiceProvider Provider = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterDiContainer();
        
        InitializeMainWindowVm();
        Provider.GetService<IRpcService>()?.Initialize();
        LibVLCSharp.Shared.Core.Initialize();

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterDiContainer()
    {
        var builder = new ServiceCollection();
        CompositionRoot.Configure(builder);
        Provider = builder.BuildServiceProvider();
    }

    private void InitializeMainWindowVm()
    {
        if(ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        
        var audioTrackLoaders = Provider.GetServices<IAudioTrackLoader>();
        var mainPlaylist = new Playlist();

        foreach(var loader in audioTrackLoaders)
        {
            mainPlaylist.AddTracks(loader.Load());
        }
        
        var mainWindowViewModel = Provider.GetRequiredService<MainWindowVm>();
        mainWindowViewModel.SetPlaylist(mainPlaylist);
        
        desktop.MainWindow = new MainWindow
        {
            DataContext = mainWindowViewModel
        };
    }
}
