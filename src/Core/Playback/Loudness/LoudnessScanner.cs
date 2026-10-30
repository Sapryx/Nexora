using Core.Logging;
using Core.Storage;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;

namespace Core.Playback.Loudness;

public class LoudnessScanner
{
    private readonly ILogger<LoudnessScanner> logger;
    private readonly ILoudnessAnalyzer loudnessAnalyzer;
    private readonly IMetadataSaver metadataSaver;
    private readonly IDegreeOfParallelismProvider degreeOfParallelismProvider;

    public LoudnessScanner(
        ILogger<LoudnessScanner> logger,
        ILoudnessAnalyzer loudnessAnalyzer,
        IMetadataSaver metadataSaver,
        IDegreeOfParallelismProvider<LoudnessScanner> degreeOfParallelismProvider)
    {
        this.logger = logger;
        this.loudnessAnalyzer = loudnessAnalyzer;
        this.metadataSaver = metadataSaver;
        this.degreeOfParallelismProvider = degreeOfParallelismProvider;
    }

    public void Scan(IEnumerable<IAudioTrack> audioTracks)
    {
        var pendingTracks = audioTracks.Where(it => it.Metadata.Loudness == null).ToList();

        if(pendingTracks.Count == 0)
        {
            return;
        }

        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = degreeOfParallelismProvider.Value };
        logger.Info($"Started analyzing loudness of {pendingTracks.Count} tracks...");

        Parallel.ForEach(pendingTracks, parallelOptions, Analyze);

        logger.Info($"Finished analyzing loudness of {pendingTracks.Count} tracks");
    }

    private void Analyze(IAudioTrack audioTrack)
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

        audioTrack.Metadata.Loudness = loudness;
        logger.Debug($"Analyzed loudness of {file}: {loudness.Lufs:0.00} LUFS");

        try
        {
            metadataSaver.SaveLoudness(file, loudness);
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to save loudness tags of {file}");
        }
    }
}
