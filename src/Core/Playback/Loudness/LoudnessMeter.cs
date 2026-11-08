using Core.Library;

namespace Core.Playback.Loudness;

public class LoudnessMeter
{
    public const int SampleRate = 44100;
    public const int Channels = 2;
    private const int SubBlockFrames = SampleRate / 10;
    private const int SubBlocksPerBlock = 4;
    private const double AbsoluteGateLufs = -70;
    private const double RelativeGateLu = -10;
    private const double LoudnessOffset = -0.691;
    private readonly List<double> subBlockPowers = [];
    private readonly KWeightingFilter[] filters = [new KWeightingFilter(), new KWeightingFilter()];
    private double subBlockSum;
    private int subBlockFrameCount;
    private float peak;

    public void Add(ReadOnlySpan<float> interleaved)
    {
        for(int i = 0; i + 1 < interleaved.Length; i += Channels)
        {
            for(int channel = 0; channel < Channels; channel++)
            {
                float sample = interleaved[i + channel];
                peak = Math.Max(peak, Math.Abs(sample));
                double filtered = filters[channel].Process(sample);
                subBlockSum += filtered * filtered;
            }

            subBlockFrameCount++;

            if(subBlockFrameCount == SubBlockFrames)
            {
                subBlockPowers.Add(subBlockSum);
                subBlockSum = 0;
                subBlockFrameCount = 0;
            }
        }
    }

    public TrackLoudness? GetResult()
    {
        var blockPowers = new List<double>();

        for(int i = 0; i + SubBlocksPerBlock <= subBlockPowers.Count; i++)
        {
            double sum = 0;

            for(int j = 0; j < SubBlocksPerBlock; j++)
            {
                sum += subBlockPowers[i + j];
            }

            blockPowers.Add(sum / (SubBlockFrames * SubBlocksPerBlock));
        }

        var aboveAbsoluteGate = blockPowers.Where(it => ToLufs(it) > AbsoluteGateLufs).ToList();

        if(aboveAbsoluteGate.Count == 0)
        {
            return null;
        }

        double relativeGate = ToLufs(aboveAbsoluteGate.Average()) + RelativeGateLu;
        var aboveRelativeGate = aboveAbsoluteGate.Where(it => ToLufs(it) > relativeGate).ToList();

        return new TrackLoudness()
        {
            Lufs = ToLufs(aboveRelativeGate.Average()),
            PeakDb = 20 * Math.Log10(peak)
        };
    }

    private static double ToLufs(double power)
    {
        return LoudnessOffset + 10 * Math.Log10(power);
    }

    private class KWeightingFilter
    {
        private const double ShelfFrequency = 1681.974450955533;
        private const double ShelfGainDb = 3.999843853973347;
        private const double ShelfQ = 0.7071752369554196;
        private const double ShelfBandGainExponent = 0.4996667741545416;
        private const double HighPassFrequency = 38.13547087602444;
        private const double HighPassQ = 0.5003270373238773;
        private readonly Biquad shelf = CreateShelf();
        private readonly Biquad highPass = CreateHighPass();

        public double Process(double sample)
        {
            return highPass.Process(shelf.Process(sample));
        }

        private static Biquad CreateShelf()
        {
            double k = Math.Tan(Math.PI * ShelfFrequency / SampleRate);
            double highGain = Math.Pow(10, ShelfGainDb / 20);
            double bandGain = Math.Pow(highGain, ShelfBandGainExponent);
            double a0 = 1 + k / ShelfQ + k * k;

            return new Biquad(
                (highGain + bandGain * k / ShelfQ + k * k) / a0,
                2 * (k * k - highGain) / a0,
                (highGain - bandGain * k / ShelfQ + k * k) / a0,
                2 * (k * k - 1) / a0,
                (1 - k / ShelfQ + k * k) / a0
            );
        }

        private static Biquad CreateHighPass()
        {
            double k = Math.Tan(Math.PI * HighPassFrequency / SampleRate);
            double a0 = 1 + k / HighPassQ + k * k;

            return new Biquad(1, -2, 1, 2 * (k * k - 1) / a0, (1 - k / HighPassQ + k * k) / a0);
        }
    }

    private class Biquad
    {
        private readonly double b0;
        private readonly double b1;
        private readonly double b2;
        private readonly double a1;
        private readonly double a2;
        private double z1;
        private double z2;

        public Biquad(double b0, double b1, double b2, double a1, double a2)
        {
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
            this.a1 = a1;
            this.a2 = a2;
        }

        public double Process(double input)
        {
            double output = b0 * input + z1;
            z1 = b1 * input - a1 * output + z2;
            z2 = b2 * input - a2 * output;

            return output;
        }
    }
}
