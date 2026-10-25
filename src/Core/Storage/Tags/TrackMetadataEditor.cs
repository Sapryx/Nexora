using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;

namespace Core.Storage.Tags;

public class TrackMetadataEditor
{
    private readonly ILogger<TrackMetadataEditor> logger;
    private readonly IMetadataSaver metadataSaver;
    private readonly IAudioPlayer audioPlayer;

    public event Action<string>? MetadataChanged;

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
            return true;
        }

        try
        {
            if(audioPlayer.NowPlaying?.AudioTrack.AudioPath == audioTrack.AudioPath)
            {
                await audioPlayer.ReleaseFileAsync(() => SaveAsync(change)).ConfigureAwait(false);
            }
            else
            {
                await SaveAsync(change).ConfigureAwait(false);
            }
        }
        catch(Exception ex)
        {
            logger.Error(ex, $"Failed to save metadata of {audioTrack.AudioPath}");
            return false;
        }

        logger.Debug($"Saved metadata of {audioTrack.AudioPath}");
        Apply(metadata, change);
        MetadataChanged?.Invoke(audioTrack.AudioPath);

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
