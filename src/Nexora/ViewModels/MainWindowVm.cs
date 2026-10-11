using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsQueueOpen { get; private set; }

    public SearchBarVm SearchBarVm { get; }
    public PlaybackVm PlaybackVm { get; }
    public QueueVm QueueVm { get; }
    private readonly ILogger logger;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        SearchBarVm searchBarVm,
        PlaybackVm playbackVm,
        QueueVm queueVm)
    {
        this.logger = logger;

        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
        QueueVm = queueVm;
    }

    public void Initialize()
    {
        logger.Info($"Application initialized");
    }

    [RelayCommand]
    public void OpenHome()
    {
        IsQueueOpen = false;
    }

    [RelayCommand]
    public void OpenQueue()
    {
        IsQueueOpen = true;
    }
}
