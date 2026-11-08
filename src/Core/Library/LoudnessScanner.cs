using Core.Logging;
using Core.Playback.Loudness;
using Core.Storage;
using Microsoft.Extensions.Logging;

namespace Core.Library;

public class LoudnessScanner
{
    private readonly ILogger<LoudnessScanner> logger;
    private readonly ILoudnessAnalyzer loudnessAnalyzer;
    private readonly TrackTagWriter tagWriter;
    private readonly IDegreeOfParallelismProvider degreeOfParallelismProvider;

    public LoudnessScanner(
        ILogger<LoudnessScanner> logger,
        ILoudnessAnalyzer loudnessAnalyzer,
        TrackTagWriter tagWriter,
        IDegreeOfParallelismProvider<LoudnessScanner> degreeOfParallelismProvider)
    {
        this.logger = logger;
        this.loudnessAnalyzer = loudnessAnalyzer;
        this.tagWriter = tagWriter;
        this.degreeOfParallelismProvider = degreeOfParallelismProvider;
    }

    public async Task ScanAsync(IEnumerable<IAudioTrack> audioTracks)
    {
        var pendingTracks = audioTracks.Where(it => it.Metadata.Loudness == null).ToList();

        if(pendingTracks.Count == 0)
        {
            return;
        }

        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = degreeOfParallelismProvider.Value };
        logger.Info($"Started analyzing loudness of {pendingTracks.Count} tracks...");

        await Parallel
            .ForEachAsync(pendingTracks, parallelOptions, async (audioTrack, cancellationToken) =>
                await AnalyzeAsync(audioTrack).ConfigureAwait(false))
            .ConfigureAwait(false);

        logger.Info($"Finished analyzing loudness of {pendingTracks.Count} tracks");
    }

    private async Task AnalyzeAsync(IAudioTrack audioTrack)
    {
        string file = audioTrack.AudioPath;
        TrackLoudness? loudness;

        try
        {
            loudness = loudnessAnalyzer.Analyze(file);
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to analyze loudness of {file}");
            return;
        }

        if(loudness == null)
        {
            logger.Debug($"Found no audible audio in {file}");
            return;
        }

        logger.Debug($"Analyzed loudness of {file}: {loudness.Lufs:0.00} LUFS");

        try
        {
            await tagWriter.SaveLoudnessAsync(audioTrack, loudness).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to save loudness tags of {file}");
        }
    }
}
