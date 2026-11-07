using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Core.Storage.Tags;

public class TrackMetadataEditor
{
    private readonly ILogger<TrackMetadataEditor> logger;
    private readonly IMetadataSaver metadataSaver;
    private readonly IAudioPlayer audioPlayer;

    public event Action<IAudioTrack>? MetadataChanged;

    public TrackMetadataEditor(
        ILogger<TrackMetadataEditor> logger,
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
        string displayedTitle = TagLibMetadataLoader.GetDisplayedTitle(title, audioTrack.AudioPath);
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
        MetadataChanged?.Invoke(audioTrack);

        return true;
    }

    private Task SaveAsync(MetadataChange change)
    {
        return Task.Run(() => metadataSaver.Save(change));
    }

    private static void Apply(Metadata metadata, MetadataChange change)
    {
        if(change.Title != null)
        {
            metadata.Title = TagLibMetadataLoader.GetDisplayedTitle(change.Title, change.AudioPath);
        }

        if(change.Artists != null)
        {
            metadata.Artists = change.Artists;
        }
    }
}
