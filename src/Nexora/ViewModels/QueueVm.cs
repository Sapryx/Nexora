using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Playlists;
using Nexora.Threading;

namespace Nexora.ViewModels;

public partial class QueueVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsEmpty { get; private set; } = true;

    public ObservableCollection<TrackControlVm> TrackVms { get; } = [];

    public QueueVm(
        PlaybackQueue playbackQueue,
        TrackControlVmRegistry trackControlVmRegistry,
        IUiDispatcher uiDispatcher)
    {
        playbackQueue.ItemAdded += playlistItem => uiDispatcher.Post(() =>
        {
            TrackVms.Add(trackControlVmRegistry.Create(playlistItem));
            IsEmpty = false;
        });

        // The dispatcher runs posted actions in FIFO order, so TrackVms already reflects all earlier queue changes,
        // and a removed item keeps the index it had, which is the index of its view model.
        playbackQueue.ItemRemoved += playlistItem => uiDispatcher.Post(() =>
        {
            TrackVms.RemoveAt(playlistItem.Index);
            IsEmpty = TrackVms.Count == 0;
        });
    }
}
