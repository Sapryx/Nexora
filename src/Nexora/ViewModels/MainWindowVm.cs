using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.ViewModels;

public partial class MainWindowVm : ViewModelBase
{
    public SearchBarVm SearchBarVm { get; }
    public PlaybackVm PlaybackVm { get; }

    private readonly ILogger logger;

    public MainWindowVm(
        ILogger<MainWindowVm> logger,
        SearchBarVm searchBarVm, PlaybackVm playbackVm)
    {
        this.logger = logger;

        SearchBarVm = searchBarVm;
        PlaybackVm = playbackVm;
    }

    public void Initialize()
    {
        logger.Info($"Application initialized");
    }
}
