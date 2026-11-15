using Core.Logging;
using Core.Playback;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;

namespace Core.Library;

public class TrackTagWriter
{
    private readonly ILogger<TrackTagWriter> logger;
    private readonly IMetadataSaver metadataSaver;
    private readonly IAudioPlayer audioPlayer;

    public event Action<IAudioTrack>? TrackChanged;

    public TrackTagWriter(
        ILogger<TrackTagWriter> logger,
        IMetadataSaver metadataSaver,
        IAudioPlayer audioPlayer)
    {
        this.logger = logger;
        this.metadataSaver = metadataSaver;
        this.audioPlayer = audioPlayer;
    }

    public async Task<bool> EditAsync(IAudioTrack audioTrack, string title, IEnumerable<string> artists)
    {
        var metadata = audioTrack.Metadata;
        title = title.Trim();
        string displayedTitle = Metadata.GetDisplayedTitle(title, audioTrack.AudioPath);
        var normalizedArtists = artists
            .Select(it => it.Trim())
            .Where(it => it != "")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var change = new MetadataChange()
        {
            AudioPath = audioTrack.AudioPath,
            Title = displayedTitle != metadata.Title ? title : null,
            Artists = normalizedArtists.SequenceEqual(metadata.Artists) ? null : normalizedArtists
        };

        if(change.Title == null && change.Artists == null)
        {
            return true;
        }

        try
        {
            await audioPlayer.ReleaseFileAsync(audioTrack.AudioPath, () => SaveAsync(change)).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            logger.Error(ex, $"Failed to save metadata of {audioTrack.AudioPath}");
            return false;
        }

        logger.Debug($"Saved metadata of {audioTrack.AudioPath}");
        Apply(metadata, change);
        TrackChanged?.Invoke(audioTrack);

        return true;
    }

    public Task SaveLoudnessAsync(IAudioTrack audioTrack, TrackLoudness loudness)
    {
        audioTrack.Metadata.Loudness = loudness;

        return audioPlayer.ReleaseFileAsync(audioTrack.AudioPath, () =>
        {
            metadataSaver.SaveLoudness(audioTrack.AudioPath, loudness);
            return Task.CompletedTask;
        });
    }

    private Task SaveAsync(MetadataChange change)
    {
        return Task.Run(() => metadataSaver.Save(change));
    }

    private static void Apply(Metadata metadata, MetadataChange change)
    {
        if(change.Title != null)
        {
            metadata.Title = Metadata.GetDisplayedTitle(change.Title, change.AudioPath);
        }

        if(change.Artists != null)
        {
            metadata.Artists = change.Artists;
        }
    }
}
