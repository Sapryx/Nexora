using CommunityToolkit.Mvvm.ComponentModel;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLibraryGridView))]
    public partial int LibraryViewIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSettingsOpen))]
    public partial int SidebarTabIndex { get; set; }

    public bool IsLibraryGridView => LibraryViewIndex == LibraryGridViewIndex;
    public bool IsSettingsOpen => SidebarTabIndex == SettingsTabIndex;
    public SearchBarVm SearchBarVm { get; }
    public PlaybackVm PlaybackVm { get; }
    public QueueVm QueueVm { get; }
    public TrackEditorVm TrackEditorVm { get; }
    private const int LibraryGridViewIndex = 1;
    private const int SettingsTabIndex = 1;
    private readonly ILogger logger;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        SearchBarVm searchBarVm,
        PlaybackVm playbackVm,
        QueueVm queueVm,
        TrackEditorVm trackEditorVm)
    {
        this.logger = logger;

        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
        QueueVm = queueVm;
        TrackEditorVm = trackEditorVm;
    }

    public void Initialize()
    {
        logger.Info($"Application initialized");
    }
}
