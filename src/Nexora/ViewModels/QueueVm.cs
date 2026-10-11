using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Playlists;
using Nexora.Threading;
using Nexora.ViewModels.Factories;

namespace Nexora.ViewModels;

public partial class QueueVm : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsEmpty { get; private set; } = true;

    public ObservableCollection<TrackControlVm> TrackVms { get; } = [];

    public QueueVm(
        PlaybackQueue playbackQueue,
        ITrackControlVmFactory trackControlVmFactory,
        IUiDispatcher uiDispatcher)
    {
        playbackQueue.ItemAdded += playlistItem => uiDispatcher.Post(() =>
        {
            TrackVms.Add(trackControlVmFactory.Create(playlistItem));
            IsEmpty = false;
        });

        // The dispatcher runs posted actions in FIFO order, so TrackVms already reflects all earlier queue changes,
        // and a removed item keeps the index it had, which is the index of its view model.
        playbackQueue.ItemRemoved += playlistItem => uiDispatcher.Post(() =>
        {
            // TODO: the removed view model stays subscribed to IAudioPlayer events, so it is never collected.
            TrackVms.RemoveAt(playlistItem.Index);
            IsEmpty = TrackVms.Count == 0;
        });
    }
}
