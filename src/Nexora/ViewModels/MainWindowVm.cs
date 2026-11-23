using CommunityToolkit.Mvvm.ComponentModel;
using Core.Logging;
using Microsoft.Extensions.Logging;
using Nexora.Media;

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
    public CoverViewerVm CoverViewerVm { get; }
    public CoverViewport QueueCovers { get; }
    public CoverViewport LibraryListCovers { get; }
    public CoverViewport LibraryGridCovers { get; }
    public bool IsPopupOpen => TrackEditorVm.IsOpen || CoverViewerVm.IsOpen;
    private const int LibraryGridViewIndex = 0;
    private const int SettingsTabIndex = 1;
    private readonly ILogger logger;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        SearchBarVm searchBarVm,
        PlaybackVm playbackVm,
        QueueVm queueVm,
        TrackEditorVm trackEditorVm,
        CoverViewerVm coverViewerVm,
        CoverMemoryBudget coverMemoryBudget)
    {
        this.logger = logger;

        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
        QueueVm = queueVm;
        TrackEditorVm = trackEditorVm;
        CoverViewerVm = coverViewerVm;
        QueueCovers = coverMemoryBudget.AddViewport();
        LibraryListCovers = coverMemoryBudget.AddViewport();
        LibraryGridCovers = coverMemoryBudget.AddViewport();
    }

    public void Initialize()
    {
        logger.Info($"Application initialized");
    }

    public void ClosePopup()
    {
        TrackEditorVm.Cancel();
        CoverViewerVm.Close();
    }
}
