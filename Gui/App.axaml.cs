using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.Integrations;
using Core.Playlists;
using Core.Storage;
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
        Provider.GetService<IRichPresenceService>()?.Initialize();
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
        
        var audioTrackLoaders = Provider.GetServices<ITrackLoader>();
        var playlistRegistry = Provider.GetService<PlaylistRegistry>()!;

        foreach(var loader in audioTrackLoaders)
        {
            Task.Run(async () =>
            {
                var loadedTracks = await loader.Load();
                playlistRegistry.GlobalPlaylist.AddTracks(loadedTracks);
            });
        }
        
        var mainWindowVm = Provider.GetRequiredService<MainWindowVm>();
        mainWindowVm.Initialize();
        
        desktop.MainWindow = new MainWindow
        {
            DataContext = mainWindowVm
        };
    }
}
