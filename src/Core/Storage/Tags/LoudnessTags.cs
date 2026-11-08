using System.Globalization;
using Core.Library;
using TagLib;
using TagLib.Id3v2;
using TagLib.Ogg;
using File = TagLib.File;

namespace Core.Storage.Tags;

public static class LoudnessTags
{
    public const string ReplayGainGainField = "REPLAYGAIN_TRACK_GAIN";
    public const string ReplayGainPeakField = "REPLAYGAIN_TRACK_PEAK";
    public const string R128GainField = "R128_TRACK_GAIN";
    private const double ReplayGainReferenceLufs = -18;
    private const double R128ReferenceLufs = -23;
    private const double R128Scale = 256;
    private const string DecibelSuffix = "dB";

    public static TrackLoudness? Read(File file)
    {
        if(file.GetTag(TagTypes.Xiph) is XiphComment xiph)
        {
            return FromReplayGain(xiph.GetFirstField(ReplayGainGainField), xiph.GetFirstField(ReplayGainPeakField))
                ?? FromR128(xiph.GetFirstField(R128GainField));
        }

        if(file.GetTag(TagTypes.Id3v2) is TagLib.Id3v2.Tag id3)
        {
            return FromReplayGain(GetUserText(id3, ReplayGainGainField), GetUserText(id3, ReplayGainPeakField));
        }

        return null;
    }

    public static void Write(File file, TrackLoudness loudness)
    {
        if(file.Properties.Codecs.OfType<TagLib.Ogg.Codecs.Opus>().Any())
        {
            var opusComment = (XiphComment)file.GetTag(TagTypes.Xiph, true);
            opusComment.SetField(R128GainField, FormatR128(loudness));
            return;
        }

        string gain = FormatReplayGainGain(loudness);
        string? peak = FormatReplayGainPeak(loudness);

        if(file is TagLib.Flac.File || file is TagLib.Ogg.File)
        {
            var xiph = (XiphComment)file.GetTag(TagTypes.Xiph, true);
            xiph.SetField(ReplayGainGainField, gain);

            if(peak != null)
            {
                xiph.SetField(ReplayGainPeakField, peak);
            }

            return;
        }

        var id3 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true);
        UserTextInformationFrame.Get(id3, ReplayGainGainField, true).Text = [gain];

        if(peak != null)
        {
            UserTextInformationFrame.Get(id3, ReplayGainPeakField, true).Text = [peak];
        }
    }

    public static TrackLoudness? FromReplayGain(string? gain, string? peak)
    {
        if(!TryParseDecibels(gain, out double gainDb))
        {
            return null;
        }

        double? peakDb = null;

        if(double.TryParse(peak, NumberStyles.Float, CultureInfo.InvariantCulture, out double peakValue) &&
           peakValue > 0)
        {
            peakDb = 20 * Math.Log10(peakValue);
        }

        return new TrackLoudness() { Lufs = ReplayGainReferenceLufs - gainDb, PeakDb = peakDb };
    }

    public static TrackLoudness? FromR128(string? gain)
    {
        if(!short.TryParse(gain, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out short value))
        {
            return null;
        }

        return new TrackLoudness() { Lufs = R128ReferenceLufs - value / R128Scale };
    }

    public static string FormatReplayGainGain(TrackLoudness loudness)
    {
        double gain = ReplayGainReferenceLufs - loudness.Lufs;

        return $"{gain.ToString("0.00", CultureInfo.InvariantCulture)} {DecibelSuffix}";
    }

    public static string? FormatReplayGainPeak(TrackLoudness loudness)
    {
        return loudness.PeakDb != null
            ? Math.Pow(10, loudness.PeakDb.Value / 20).ToString("0.000000", CultureInfo.InvariantCulture)
            : null;
    }

    public static string FormatR128(TrackLoudness loudness)
    {
        double value = Math.Round((R128ReferenceLufs - loudness.Lufs) * R128Scale);

        return ((short)Math.Clamp(value, short.MinValue, short.MaxValue)).ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseDecibels(string? text, out double value)
    {
        value = 0;

        if(text == null)
        {
            return false;
        }

        string trimmed = text.Trim();

        if(trimmed.EndsWith(DecibelSuffix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - DecibelSuffix.Length).TrimEnd();
        }

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string? GetUserText(TagLib.Id3v2.Tag tag, string description)
    {
        return UserTextInformationFrame
            .Get(tag, description, TagLib.Id3v2.Tag.DefaultEncoding, false, false)?
            .Text
            .FirstOrDefault();
    }
}
