using Core.Library;
using LibVLCSharp.Shared;

namespace Core.Playback.Loudness;

public class VlcLoudnessAnalyzer : ILoudnessAnalyzer
{
    private const int ReadBufferBytes = LoudnessMeter.SampleRate * LoudnessMeter.Channels * sizeof(short);
    private const float SampleScale = 1f / 32768f;
    private static readonly TimeSpan DecodeTimeout = TimeSpan.FromMinutes(2);
    private readonly LibVLC vlc = new LibVLC("--no-video", "--aout=adummy");

    public TrackLoudness? Analyze(string audioPath)
    {
        string pcmPath = Path.Combine(Path.GetTempPath(), $"nexora-loudness-{Guid.NewGuid()}.pcm");

        try
        {
            Decode(audioPath, pcmPath);

            return Measure(pcmPath);
        }
        finally
        {
            File.Delete(pcmPath);
        }
    }

    private void Decode(string audioPath, string pcmPath)
    {
        using var media = new Media(vlc, audioPath, FromType.FromPath);
        media.AddOption(":no-sout-video");
        media.AddOption(
            $":sout=#transcode{{acodec=s16l,channels={LoudnessMeter.Channels}," +
            $"samplerate={LoudnessMeter.SampleRate}}}:std{{access=file,mux=raw,dst={pcmPath}}}"
        );

        using var player = new MediaPlayer(media);
        using var finished = new ManualResetEventSlim();
        bool hasFailed = false;
        player.EndReached += (_, _) => finished.Set();
        player.EncounteredError += (_, _) =>
        {
            hasFailed = true;
            finished.Set();
        };

        if(!player.Play())
        {
            throw new InvalidOperationException($"LibVLC failed to open {audioPath}");
        }

        bool isCompleted = finished.Wait(DecodeTimeout);
        player.Stop();

        if(!isCompleted)
        {
            throw new TimeoutException($"Decoding {audioPath} for loudness analysis timed out");
        }

        if(hasFailed)
        {
            throw new InvalidOperationException($"LibVLC failed to decode {audioPath}");
        }
    }

    private static TrackLoudness? Measure(string pcmPath)
    {
        var meter = new LoudnessMeter();
        var bytes = new byte[ReadBufferBytes];
        var samples = new float[ReadBufferBytes / sizeof(short)];

        using var stream = File.OpenRead(pcmPath);
        int read;

        while((read = stream.ReadAtLeast(bytes, bytes.Length, false)) > 0)
        {
            int count = read / sizeof(short);

            for(int i = 0; i < count; i++)
            {
                samples[i] = BitConverter.ToInt16(bytes, i * sizeof(short)) * SampleScale;
            }

            meter.Add(samples.AsSpan(0, count));
        }

        return meter.GetResult();
    }
}
