using Core.Playback.Loudness;
using Core.Storage.Tags;

namespace Core.Tests.Storage.Tags;

public class LoudnessTagsTests
{
    [Fact]
    public void FromR128_NegativeValue_ReturnsLoudnessAboveReference()
    {
        var loudness = LoudnessTags.FromR128("-1536");

        Assert.Equal(-17, loudness!.Lufs, 6);
        Assert.Null(loudness.PeakDb);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-6 dB")]
    [InlineData("99999")]
    public void FromR128_InvalidValue_ReturnsNull(string? value)
    {
        Assert.Null(LoudnessTags.FromR128(value));
    }

    [Fact]
    public void FromReplayGain_GainWithUnitAndPeak_ReturnsLoudnessAndPeak()
    {
        var loudness = LoudnessTags.FromReplayGain("-6.50 dB", "0.5");

        Assert.Equal(-11.5, loudness!.Lufs, 6);
        Assert.Equal(-6.0206, loudness.PeakDb!.Value, 3);
    }

    [Fact]
    public void FromReplayGain_GainWithoutUnit_ReturnsLoudness()
    {
        Assert.Equal(-20, LoudnessTags.FromReplayGain("+2", null)!.Lufs, 6);
    }

    [Fact]
    public void FromReplayGain_MissingPeak_ReturnsLoudnessWithoutPeak()
    {
        Assert.Null(LoudnessTags.FromReplayGain("-1 dB", null)!.PeakDb);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("loud")]
    public void FromReplayGain_InvalidGain_ReturnsNull(string? gain)
    {
        Assert.Null(LoudnessTags.FromReplayGain(gain, "1.0"));
    }

    [Fact]
    public void FormatR128_Loudness_RoundTripsThroughFromR128()
    {
        var loudness = new TrackLoudness() { Lufs = -9.25 };

        string value = LoudnessTags.FormatR128(loudness);

        Assert.Equal("-3520", value);
        Assert.Equal(-9.25, LoudnessTags.FromR128(value)!.Lufs, 6);
    }

    [Fact]
    public void FormatR128_ExtremeLoudness_ClampsToShortRange()
    {
        Assert.Equal("32767", LoudnessTags.FormatR128(new TrackLoudness() { Lufs = -200 }));
    }

    [Fact]
    public void FormatReplayGain_Loudness_RoundTripsThroughFromReplayGain()
    {
        var loudness = new TrackLoudness() { Lufs = -9.25, PeakDb = -6.0206 };

        string gain = LoudnessTags.FormatReplayGainGain(loudness);
        string? peak = LoudnessTags.FormatReplayGainPeak(loudness);
        var parsed = LoudnessTags.FromReplayGain(gain, peak);

        Assert.Equal("-8.75 dB", gain);
        Assert.Equal("0.500000", peak);
        Assert.Equal(-9.25, parsed!.Lufs, 6);
        Assert.Equal(-6.0206, parsed.PeakDb!.Value, 3);
    }

    [Fact]
    public void FormatReplayGainPeak_NoPeak_ReturnsNull()
    {
        Assert.Null(LoudnessTags.FormatReplayGainPeak(new TrackLoudness() { Lufs = -14 }));
    }
}
