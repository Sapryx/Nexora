using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Logging;
using Core.Storage;
using Microsoft.Extensions.Logging;

namespace Nexora.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsQueueOpen { get; private set; }

    [ObservableProperty]
    public partial bool IsLibraryGridView { get; private set; }

    public SearchBarVm SearchBarVm { get; }
    public PlaybackVm PlaybackVm { get; }
    public QueueVm QueueVm { get; }
    private readonly ILogger logger;
    private readonly TrackMetadataEditor metadataEditor;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        SearchBarVm searchBarVm,
        PlaybackVm playbackVm,
        QueueVm queueVm,
        TrackMetadataEditor metadataEditor)
    {
        this.logger = logger;
        this.metadataEditor = metadataEditor;

        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
        QueueVm = queueVm;
    }

    public void Initialize()
    {
        logger.Info($"Application initialized");
    }

    [RelayCommand]
    public void OpenLibrary()
    {
        IsQueueOpen = false;
    }

    [RelayCommand]
    public void OpenQueue()
    {
        IsQueueOpen = true;
    }

    [RelayCommand]
    public Task SaveMetadata()
    {
        return metadataEditor.SaveAllAsync();
    }

    [RelayCommand]
    public void ShowLibraryList()
    {
        IsLibraryGridView = false;
    }

    [RelayCommand]
    public void ShowLibraryGrid()
    {
        IsLibraryGridView = true;
    }
}
