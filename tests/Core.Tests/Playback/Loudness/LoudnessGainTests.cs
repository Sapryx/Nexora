using Core.Playback.Loudness;

namespace Core.Tests.Playback.Loudness;

public class LoudnessGainTests
{
    [Fact]
    public void Calculate_LouderThanTarget_ReturnsNegativeGain()
    {
        var loudness = new TrackLoudness() { Lufs = -8, PeakDb = 0 };

        Assert.Equal(-6, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_QuieterThanTargetWithHeadroom_ReturnsFullGain()
    {
        var loudness = new TrackLoudness() { Lufs = -20, PeakDb = -10 };

        Assert.Equal(6, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_QuieterThanTargetWithLittleHeadroom_LimitsGainByPeak()
    {
        var loudness = new TrackLoudness() { Lufs = -20, PeakDb = -5 };

        Assert.Equal(4, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_QuieterThanTargetWithPeakAboveCeiling_ReturnsZero()
    {
        var loudness = new TrackLoudness() { Lufs = -20, PeakDb = -0.1 };

        Assert.Equal(0, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_QuieterThanTargetWithoutPeak_ReturnsZero()
    {
        var loudness = new TrackLoudness() { Lufs = -20 };

        Assert.Equal(0, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_LouderThanTargetWithoutPeak_ReturnsNegativeGain()
    {
        var loudness = new TrackLoudness() { Lufs = -10 };

        Assert.Equal(-4, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_FarLouderThanTarget_ClampsToMinusTwenty()
    {
        var loudness = new TrackLoudness() { Lufs = 10, PeakDb = 0 };

        Assert.Equal(-20, LoudnessGain.Calculate(loudness, -14), 6);
    }

    [Fact]
    public void Calculate_FarQuieterThanTarget_ClampsToTwenty()
    {
        var loudness = new TrackLoudness() { Lufs = -50, PeakDb = -40 };

        Assert.Equal(20, LoudnessGain.Calculate(loudness, -14), 6);
    }
}
