using Core.Library;
using Core.Logging;
using Core.Tracks;
using Microsoft.Extensions.Logging;

namespace Nexora.Media;

public sealed class ThumbnailGenerator : IDisposable
{
    public Task Completion => generation ?? Task.CompletedTask;
    private readonly ILogger<ThumbnailGenerator> logger;
    private readonly MusicLibrary library;
    private readonly ICoverCache coverCache;
    private readonly IThumbnailStore thumbnailStore;
    private readonly CancellationTokenSource stopping = new CancellationTokenSource();
    private readonly Lock lifecycleLock = new Lock();
    private bool started;
    private Task? generation;

    public ThumbnailGenerator(
        ILogger<ThumbnailGenerator> logger,
        MusicLibrary library,
        ICoverCache coverCache,
        IThumbnailStore thumbnailStore)
    {
        this.logger = logger;
        this.library = library;
        this.coverCache = coverCache;
        this.thumbnailStore = thumbnailStore;
    }

    public void Start()
    {
        lock(lifecycleLock)
        {
            if(started || stopping.IsCancellationRequested)
            {
                return;
            }

            started = true;
            library.Loaded += OnLibraryLoaded;
        }
    }

    public void Dispose()
    {
        lock(lifecycleLock)
        {
            if(!stopping.IsCancellationRequested)
            {
                library.Loaded -= OnLibraryLoaded;
                stopping.Cancel();
            }

            Completion.GetAwaiter().GetResult();
            stopping.Dispose();
        }
    }

    private void OnLibraryLoaded(IReadOnlyList<IAudioTrack> tracks)
    {
        lock(lifecycleLock)
        {
            if(stopping.IsCancellationRequested || generation != null)
            {
                return;
            }

            library.Loaded -= OnLibraryLoaded;
            generation = Task.Factory.StartNew(
                () => CreateAll(tracks),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }
    }

    private void CreateAll(IReadOnlyList<IAudioTrack> tracks)
    {
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        logger.Info($"Started creating thumbnails of {tracks.Count} tracks...");

        try
        {
            thumbnailStore.RemoveOrphanedThumbnails(tracks.Select(track => track.AudioPath), stopping.Token);

            foreach(var track in tracks)
            {
                stopping.Token.ThrowIfCancellationRequested();
                coverCache.CreateThumbnails(track.AudioPath, stopping.Token);
            }

            logger.Info($"Finished creating thumbnails of {tracks.Count} tracks");
        }
        catch(OperationCanceledException)
        {
            logger.Info($"Stopped creating thumbnails");
        }
    }
}
