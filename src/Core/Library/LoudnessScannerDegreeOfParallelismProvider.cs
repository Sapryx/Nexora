using Core.Storage;

namespace Core.Library;

public class LoudnessScannerDegreeOfParallelismProvider : IDegreeOfParallelismProvider<LoudnessScanner>
{
    public int Value { get; } = Math.Max(1, Environment.ProcessorCount / 2);
}
