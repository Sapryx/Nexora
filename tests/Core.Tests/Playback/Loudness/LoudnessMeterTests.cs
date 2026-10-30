using Core.Playback.Loudness;

namespace Core.Tests.Playback.Loudness;

public class LoudnessMeterTests
{
    private const double Tolerance = 0.05;

    [Fact]
    public void GetResult_FullScaleSineInBothChannels_ReturnsZeroLufs()
    {
        var meter = new LoudnessMeter();

        meter.Add(CreateSine(1.0, true, true));

        Assert.Equal(0.0, meter.GetResult()!.Lufs, Tolerance);
    }

    [Fact]
    public void GetResult_FullScaleSineInOneChannel_ReturnsMinusThreeLufs()
    {
        var meter = new LoudnessMeter();

        meter.Add(CreateSine(1.0, true, false));

        Assert.Equal(-3.01, meter.GetResult()!.Lufs, Tolerance);
    }

    [Fact]
    public void GetResult_SineAtMinusTwentyDecibels_ReturnsMinusTwentyLufs()
    {
        var meter = new LoudnessMeter();

        meter.Add(CreateSine(0.1, true, true));

        Assert.Equal(-20.0, meter.GetResult()!.Lufs, Tolerance);
    }

    [Fact]
    public void GetResult_Sine_ReturnsSamplePeak()
    {
        var meter = new LoudnessMeter();

        meter.Add(CreateSine(0.5, true, true));

        Assert.Equal(-6.02, meter.GetResult()!.PeakDb!.Value, Tolerance);
    }

    [Fact]
    public void GetResult_SilenceBeforeSine_GatesSilenceOut()
    {
        var meter = new LoudnessMeter();

        meter.Add(new float[LoudnessMeter.SampleRate * LoudnessMeter.Channels * 3]);
        meter.Add(CreateSine(1.0, true, true));

        Assert.Equal(-0.13, meter.GetResult()!.Lufs, Tolerance);
    }

    [Fact]
    public void GetResult_Silence_ReturnsNull()
    {
        var meter = new LoudnessMeter();

        meter.Add(new float[LoudnessMeter.SampleRate * LoudnessMeter.Channels * 3]);

        Assert.Null(meter.GetResult());
    }

    [Fact]
    public void GetResult_ShorterThanOneBlock_ReturnsNull()
    {
        var meter = new LoudnessMeter();

        meter.Add(CreateSine(1.0, true, true).Take(LoudnessMeter.SampleRate / 10).ToArray());

        Assert.Null(meter.GetResult());
    }

    private static float[] CreateSine(double amplitude, bool left, bool right)
    {
        const double frequency = 997;
        int frames = LoudnessMeter.SampleRate * 5;
        var samples = new float[frames * LoudnessMeter.Channels];

        for(int i = 0; i < frames; i++)
        {
            float sample = (float)(amplitude * Math.Sin(2 * Math.PI * frequency * i / LoudnessMeter.SampleRate));
            samples[i * 2] = left ? sample : 0;
            samples[i * 2 + 1] = right ? sample : 0;
        }

        return samples;
    }
}
