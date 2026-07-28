using BenchmarkDotNet.Attributes;
using Core.Storage;
using Infrastructure.Storage;

namespace Core.Benchmarks;

[MemoryDiagnoser]
public class FileTrackLoaderBenchmark
{
    [Params(16, 32, 64)]
    public int DegreeOfParallelism { get; set; }

    private FileTrackLoader loader = null!;
    
    private class FixedDegreeOfParallelismProvider : IDegreeOfParallelismProvider<FileTrackLoader>
    {
        public required int Value { get; init; }
    }

    [GlobalSetup]
    public void Setup()
    {
        var degreeOfParallelismProvider = new FixedDegreeOfParallelismProvider { Value = DegreeOfParallelism };

        loader = new FileTrackLoader(
            new FakeLogger<FileTrackLoader>(),
            new TagLibMetadataLoader(),
            new TagLibTrackPropertyLoader(),
            new SupportedAudioFormatsProvider(),
            degreeOfParallelismProvider);
    }

    [Benchmark]
    public void LoadTracks()
    {
        loader.Load();
    }
}
