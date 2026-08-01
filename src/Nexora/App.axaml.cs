using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.Integrations;
using Core.Logging;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Logging;
using Nexora.ViewModels;
using Nexora.Views;

namespace Nexora;

public partial class App : Application
{
    private static ServiceProvider Provider = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = new ServiceCollection();
        LoggingInitializer.Initialize(builder);
        RegisterDiContainer(builder);
        InitializeMainWindowVm();
        Provider.GetService<IRichPresenceService>()?.Initialize();
        LibVLCSharp.Shared.Core.Initialize();

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterDiContainer(ServiceCollection builder)
    {
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

        var mainWindowVm = Provider.GetRequiredService<MainWindowVm>();
        mainWindowVm.Initialize();

        desktop.MainWindow = new MainWindow
        {
            DataContext = mainWindowVm
        };

        var logger = Provider.GetService<ILogger<App>>()!;

        // foreach(var loader in audioTrackLoaders)
        // {
        //     Task.Run(() =>
        //     {
        //         try
        //         {
        //             var loadedTracks = loader.Load();
        //             playlistRegistry.GlobalPlaylist.AddTracks(loadedTracks);
        //         }
        //         catch(Exception ex)
        //         {
        //             logger.Crit(ex, $"");
        //         }
        //     });
        // }
    }
}
