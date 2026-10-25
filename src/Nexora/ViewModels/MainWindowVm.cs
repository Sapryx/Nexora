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
    [NotifyPropertyChangedFor(nameof(IsLibraryGridView))]
    public partial int LibraryViewIndex { get; set; }

    public bool IsLibraryGridView => LibraryViewIndex == LibraryGridViewIndex;
    public SearchBarVm SearchBarVm { get; }
    public PlaybackVm PlaybackVm { get; }
    public QueueVm QueueVm { get; }
    public TrackEditorVm TrackEditorVm { get; }
    private const int LibraryGridViewIndex = 1;
    private readonly ILogger logger;
    private readonly TrackMetadataEditor metadataEditor;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        SearchBarVm searchBarVm,
        PlaybackVm playbackVm,
        QueueVm queueVm,
        TrackEditorVm trackEditorVm,
        TrackMetadataEditor metadataEditor)
    {
        this.logger = logger;
        this.metadataEditor = metadataEditor;

        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
        QueueVm = queueVm;
        TrackEditorVm = trackEditorVm;
    }

    public void Initialize()
    {
        logger.Info($"Application initialized");
    }

    [RelayCommand]
    public async Task SaveMetadata()
    {
        await TrackEditorVm.ApplyAsync();
        await metadataEditor.SaveAllAsync();
    }
}
