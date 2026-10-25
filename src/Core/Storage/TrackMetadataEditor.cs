using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class TrackMetadataEditor
{
    private readonly ILogger<TrackMetadataEditor> logger;
    private readonly IMetadataSaver metadataSaver;
    private readonly IPendingMetadataStorage pendingStorage;
    private readonly IAudioPlayer audioPlayer;
    private readonly Dictionary<string, MetadataChange> pendingChanges = [];
    private readonly Lock pendingLock = new Lock();
    // Saves never overlap: a second TagLib write to the same file would fail on the first one's lock.
    private readonly SemaphoreSlim saveSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>Raised with the audio path when a track's metadata or its unsaved state changes.</summary>
    public event Action<string>? MetadataChanged;

    public TrackMetadataEditor(
        ILogger<TrackMetadataEditor> logger,
        IMetadataSaver metadataSaver,
        IPendingMetadataStorage pendingStorage,
        IAudioPlayer audioPlayer)
    {
        this.logger = logger;
        this.metadataSaver = metadataSaver;
        this.pendingStorage = pendingStorage;
        this.audioPlayer = audioPlayer;

        // Player events come from LibVLC threads, which must not call back into LibVLC.
        audioPlayer.PlaybackStarted += playlistItem =>
            RunInBackground(() => SavePendingExceptAsync(playlistItem.AudioTrack.AudioPath));
        audioPlayer.PlaybackStopped += () => RunInBackground(() => SavePendingExceptAsync(null));
        audioPlayer.PlaybackPaused += () => RunInBackground(SaveLoadedTrackAsync);
    }

    public bool HasUnsavedChanges(string audioPath)
    {
        lock(pendingLock)
        {
            return pendingChanges.ContainsKey(audioPath);
        }
    }

    /// <summary>
    /// Writes the changes left pending by the previous launch to the files. Changes that still can't be written stay
    /// pending; <see cref="ApplyPendingChanges"/> shows them on the loaded tracks.
    /// </summary>
    public void RecoverPendingChanges()
    {
        foreach(var change in pendingStorage.LoadAll())
        {
            try
            {
                metadataSaver.Save(change);
                DeletePendingFile(change.AudioPath);
                logger.Info($"Applied pending metadata change of {change.AudioPath}");
            }
            catch(Exception ex) when(ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                logger.Warn($"Dropped pending metadata change of missing file {change.AudioPath}");
                DeletePendingFile(change.AudioPath);
            }
            catch(Exception ex)
            {
                logger.Warn(ex, $"Failed to apply pending metadata change of {change.AudioPath}, keeping it");

                lock(pendingLock)
                {
                    pendingChanges[change.AudioPath] = change;
                }
            }
        }
    }

    public void ApplyPendingChanges(IEnumerable<IAudioTrack> audioTracks)
    {
        lock(pendingLock)
        {
            foreach(var audioTrack in audioTracks)
            {
                if(pendingChanges.TryGetValue(audioTrack.AudioPath, out var change))
                {
                    Apply(audioTrack.Metadata, change);
                }
            }
        }
    }

    /// <summary>
    /// Shows the new metadata at once and writes it to the file, or keeps it pending while the player holds the file.
    /// </summary>
    public async Task EditAsync(IAudioTrack audioTrack, string title, IEnumerable<string> artists)
    {
        var metadata = audioTrack.Metadata;
        title = title.Trim();
        var normalizedArtists = artists
            .Select(it => it.Trim())
            .Where(it => it != "")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var change = new MetadataChange()
        {
            AudioPath = audioTrack.AudioPath,
            Title = GetDisplayedTitle(title, audioTrack.AudioPath) != metadata.Title ? title : null,
            Artists = normalizedArtists.SequenceEqual(metadata.Artists) ? null : normalizedArtists
        };

        if(change.Title == null && change.Artists == null)
        {
            return;
        }

        Apply(metadata, change);
        AddPendingChange(change);
        MetadataChanged?.Invoke(audioTrack.AudioPath);

        if(IsLoadedInPlayer(audioTrack.AudioPath))
        {
            logger.Debug($"Postponed saving metadata of {audioTrack.AudioPath} while it is playing");
        }
        else
        {
            await SavePendingAsync(audioTrack.AudioPath).ConfigureAwait(false);
        }
    }

    /// <summary>Writes every pending change, briefly releasing the playing track's file if it has one.</summary>
    public async Task SaveAllAsync()
    {
        foreach(string audioPath in GetPendingPaths())
        {
            if(IsLoadedInPlayer(audioPath))
            {
                await audioPlayer.ReleaseFileAsync(() => SavePendingAsync(audioPath)).ConfigureAwait(false);
            }
            else
            {
                await SavePendingAsync(audioPath).ConfigureAwait(false);
            }
        }
    }

    private async Task SaveLoadedTrackAsync()
    {
        string? audioPath = audioPlayer.NowPlaying?.AudioTrack.AudioPath;

        if(audioPath != null && HasUnsavedChanges(audioPath))
        {
            await audioPlayer.ReleaseFileAsync(() => SavePendingAsync(audioPath)).ConfigureAwait(false);
        }
    }

    private async Task SavePendingExceptAsync(string? loadedAudioPath)
    {
        foreach(string audioPath in GetPendingPaths())
        {
            if(audioPath != loadedAudioPath)
            {
                await SavePendingAsync(audioPath).ConfigureAwait(false);
            }
        }
    }

    private async Task SavePendingAsync(string audioPath)
    {
        await saveSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            MetadataChange? change;

            lock(pendingLock)
            {
                pendingChanges.TryGetValue(audioPath, out change);
            }

            if(change == null)
            {
                return;
            }

            try
            {
                await Task.Run(() => metadataSaver.Save(change)).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                logger.Error(ex, $"Failed to save metadata of {audioPath}");
                return;
            }

            bool isUpToDate;

            lock(pendingLock)
            {
                // An edit made during the save replaced the change and stays pending for the next save.
                isUpToDate = pendingChanges.TryGetValue(audioPath, out var current) && current == change;

                if(isUpToDate)
                {
                    pendingChanges.Remove(audioPath);
                }
            }

            logger.Debug($"Saved metadata of {audioPath}");

            if(isUpToDate)
            {
                DeletePendingFile(audioPath);
                MetadataChanged?.Invoke(audioPath);
            }
        }
        finally
        {
            saveSemaphore.Release();
        }
    }

    private void AddPendingChange(MetadataChange change)
    {
        MetadataChange merged;

        lock(pendingLock)
        {
            pendingChanges.TryGetValue(change.AudioPath, out var previous);
            merged = new MetadataChange()
            {
                AudioPath = change.AudioPath,
                Title = change.Title ?? previous?.Title,
                Artists = change.Artists ?? previous?.Artists
            };
            pendingChanges[change.AudioPath] = merged;
        }

        try
        {
            pendingStorage.Save(merged);
        }
        catch(Exception ex)
        {
            logger.Error(ex, $"Failed to store pending metadata change of {change.AudioPath}");
        }
    }

    private void DeletePendingFile(string audioPath)
    {
        try
        {
            pendingStorage.Delete(audioPath);
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to delete pending metadata change of {audioPath}");
        }
    }

    private List<string> GetPendingPaths()
    {
        lock(pendingLock)
        {
            return pendingChanges.Keys.ToList();
        }
    }

    private bool IsLoadedInPlayer(string audioPath)
    {
        return audioPlayer.NowPlaying?.AudioTrack.AudioPath == audioPath;
    }

    private void RunInBackground(Func<Task> action)
    {
        Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                logger.Error(ex, $"Failed to save pending metadata changes");
            }
        });
    }

    private static void Apply(Metadata metadata, MetadataChange change)
    {
        if(change.Title != null)
        {
            metadata.Title = GetDisplayedTitle(change.Title, change.AudioPath);
        }

        if(change.Artists != null)
        {
            metadata.Artists = change.Artists;
        }
    }

    // Same fallback as TagLibMetadataLoader, so a cleared title shows the file name now as after a restart.
    private static string GetDisplayedTitle(string title, string audioPath)
    {
        return title == "" ? Path.GetFileNameWithoutExtension(audioPath) : title;
    }
}
